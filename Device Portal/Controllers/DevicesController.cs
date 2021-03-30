using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevicePortal.Data;

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

        // GET: api/Devices/Me
        [HttpGet("me")]
        public async Task<ActionResult<IEnumerable<Device>>> GetMyDevices()
        {
            string userId = User.GetUserName();
            if (string.IsNullOrEmpty(userId)) { return BadRequest("UserId not available"); }

            var user = _context.Users.Find(userId);
            if (user == null) { return NotFound(); }

            await _intuneService.SyncManagedDeviceUser(user.UserName, user.ObjectId);
            return await _context.Devices.Where(d => d.UserName == userId).Active().ToListAsync();
        }

        // GET: api/Devices/User{userName}
        [HttpGet("User/{userName}")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices(string userName)
        {
            return await _context.Devices.Where(d => d.UserName == userName).Active().ToListAsync();
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
            _context.DeviceHistories.Add(history);

            // Only allow status update changes if device is lost
            if (device.Status == DeviceStatus.Lost) { device.StatusEffectiveDate = DateTime.Now; }
            else { device.Status = original.Status; }

            _context.UpdateProperties(device, d => d.Name, d=> d.OS_Type, d => d.OS_Version, d => d.Status, d => d.Disowned,
                                              d => d.StatusEffectiveDate);
            
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
