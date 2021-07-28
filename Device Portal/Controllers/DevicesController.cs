using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly PortalContext _context;
        private readonly IntuneService _intuneService;

        public DevicesController(PortalContext context, IntuneService intuneService)
        {
            _context = context;
            _intuneService = intuneService;
        }

        // GET: api/Devices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
        {
            return await _context.Devices.Active().ToListAsync();
        }

        // GET: api/Devices/Count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetDeviceCount()
        {
            return await _context.Devices.Active().CountAsync();
        }

        // GET: api/Devices/Changes
        [HttpGet("changes")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<ActionResult<IEnumerable<Device>>> GetChanges()
        {
            var devices = await _context.Devices.Active()
                .Include(d => d.User)
                .Where(d => d.UserEditId != Data.User.ImporterId || d.History.Any(h => h.UserEditId != Data.User.ImporterId))
                .ToListAsync();
            var userEditIds = devices.Where(d => d.UserEditId != null).Select(d => d.UserEditId);
            var editNameMap = await _context.Users
                .Where(u => userEditIds.Contains(u.UserName))
                .Select(u => new { u.UserName, u.Name })
                .ToDictionaryAsync(u => u.UserName, u => u.Name);
            foreach (var d in devices)
            {
                d.UserEditName = (d.UserEditId == null || !editNameMap.TryGetValue(d.UserEditId, out var name)) ? null : name;
            }
            return devices;
        }

        // GET: api/Devices/Me
        [HttpGet("me")]
        public async Task<ActionResult<IEnumerable<Device>>> GetMyDevices()
        {
            string userId = User.GetUserName();
            if (string.IsNullOrEmpty(userId)) { return BadRequest("UserId not available"); }

            var user = _context.Users.Find(userId);
            if (user == null) { return NotFound(); }

            if (!string.IsNullOrEmpty(user.Email) && string.IsNullOrEmpty(user.ObjectId))
            {
                user.ObjectId = await _intuneService.GetUserObjectId(user.Email);
                if (!string.IsNullOrEmpty(user.ObjectId))
                {
                    var entry = _context.Entry(user);
                    entry.Property(u => u.ObjectId).IsModified = true;
                    _context.SaveChanges();
                }
            }
            await _intuneService.SyncManagedDeviceUser(user.UserName, user.ObjectId);
            return await _context.Devices.Where(d => d.UserName == userId).Active().ToListAsync();
        }

        // GET: api/Devices/User{userName}
        [HttpGet("User/{userName}")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices(string userName)
        {
            var devices = await _context.Devices.Include(d => d.User).Where(d => d.UserName == userName).Active().ToListAsync();
            var userEditIds = devices.Where(d => d.UserEditId != null).Select(d => d.UserEditId);
            var editNameMap = await _context.Users
                .Where(u => userEditIds.Contains(u.UserName))
                .Select(u => new { u.UserName, u.Name })
                .ToDictionaryAsync(u => u.UserName, u => u.Name);
            foreach (var d in devices)
            {
                d.UserEditName = (d.UserEditId == null || !editNameMap.TryGetValue(d.UserEditId, out var name)) ? null : name;
            }
            return devices;
        }

        // GET: api/Devices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Device>> GetDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null || device.Status == DeviceStatus.Disposed)
            {
                return NotFound();
            }

            return device;
        }

        // GET: api/Devices/5/History
        [HttpGet("{id}/History")]
        public async Task<ActionResult<DeviceHistory[]>> GetDeviceHistory(int id)
        {
            if (!_context.Devices.Any(d => d.Id == id && d.Status != DeviceStatus.Disposed))
            {
                return NotFound();
            }

            var history = await _context.DeviceHistories
                .Include(h => h.User)
                .Where(h => h.OriginalDeviceId == id)
                .OrderByDescending(h => h.Id)
                .ToArrayAsync();
            var userIds = history.Where(h => h.UserEditId != null).Select(h => h.UserEditId).ToHashSet();
            var nameMap = await _context.Users.Where(u => userIds.Contains(u.UserName)).Select(u => new { u.UserName, u.Name }).ToDictionaryAsync(u => u.UserName, u => u.Name);
            foreach (var h in history)
            {
                h.UserEditName = (h.UserEditId == null || !nameMap.TryGetValue(h.UserEditId, out var name)) ? null : name;
            }
            return history;
        }

        // PUT: api/Devices/5        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDevice(int id, Device device)
        {
            if (id != device.Id)
            {
                return BadRequest();
            }

            var original = await _context.Devices.FindAsync(id);
            var history = new DeviceHistory(original);

            // Only allow status update changes if device is lost
            if (device.Status == DeviceStatus.Lost) { device.StatusEffectiveDate = DateTime.Now; }
            else { device.Status = original.Status; }

            bool canManage = User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManage);

            if (original.Name == device.Name &&
                original.OS_Type == device.OS_Type &&
                original.OS_Version == device.OS_Version &&
                original.Status == device.Status &&
                original.Disowned == device.Disowned &&
                original.StatusEffectiveDate == device.StatusEffectiveDate &&
                (!canManage ||
                original.Notes == device.Notes &&
                original.Shared == device.Shared &&
                original.ItracsBuilding == device.ItracsBuilding &&
                original.ItracsRoom == device.ItracsRoom &&
                original.ItracsOutlet == device.ItracsOutlet &&
                original.UserName == device.UserName))
            {
                // No changes, prevent adding unnecessary history entries
                return NoContent();
            }

            _context.DeviceHistories.Add(history);

            device.DateEdit = DateTime.Now;
            device.UserEditId = User.GetUserName();
            _context.UpdateProperties(device, d => d.Name, d=> d.OS_Type, d => d.OS_Version, d => d.Status, d => d.Disowned,
                                              d => d.StatusEffectiveDate, d => d.UserEditId, d => d.DateEdit);
            if (canManage)
            {
                _context.UpdateProperties(device, d => d.Notes, d => d.Shared, d => d.ItracsBuilding, d => d.ItracsRoom,
                    d => d.ItracsOutlet, d => d.UserName);
            }
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeviceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Devices
        [HttpPost]
        public async Task<ActionResult<Device>> PostDevice(Device device)
        {
            int[] departmentIds = User.GetDepartmentIds(_context);
            if (departmentIds.Length == 0) { return BadRequest("Failed to save device. There's no department to add device to."); }

            // TODO: should probably not be simply set to the first in case of multiple departments
            device.DepartmentId = departmentIds[0];
            device.Status = DeviceStatus.Unsecure;
            device.StatusEffectiveDate = DateTime.Now;
            device.Origin = DeviceOrigin.User;
            device.Category = DeviceCategory.BYOD;
            device.UserName = User.GetUserName();
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDevice", new { id = device.Id }, device);
        }

        // DELETE: api/Devices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeviceExists(int id)
        {
            return _context.Devices.Any(e => e.Id == id);
        }
    }
}
