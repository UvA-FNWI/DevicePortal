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

        public DevicesController(PortalContext context)
        {
            _context = context;
        }

        // GET: api/Devices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
        {
            return await _context.Devices.ToListAsync();
        }

        // GET: api/Devices/Me
        [HttpGet("me")]
        public async Task<ActionResult<IEnumerable<Device>>> GetMyDevices()
        {
            string userId = User.GetUserName();
            if (string.IsNullOrEmpty(userId)) { return BadRequest("UserId not availaable"); }

            return await _context.Devices.Where(d => d.UserName == userId).ToListAsync();
        }

        // GET: api/Devices/User{userName}
        [HttpGet("User/{userName}")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices(string userName)
        {
            return await _context.Devices.Where(d => d.UserName == userName).ToListAsync();
        }

        // GET: api/Devices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Device>> GetDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
            {
                return NotFound();
            }

            return device;
        }

        // GET: api/Devices/5/SecurityCheck
        [HttpGet("{id}/SecurityCheck")]
        public async Task<ActionResult<SecurityCheck>> GetSecurityCheck(int id)
        {
            var securityCheck = await _context.SecurityChecks
                .Include(c => c.Questions)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(c => c.DeviceId == id);

            if (securityCheck == null)
            {
                return NotFound();
            }

            return securityCheck;
        }

        // PUT: api/Devices/5        
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutDevice(int id, Device device)
        //{
        //    if (id != device.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(device).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!DeviceExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/Devices
        [HttpPost]
        public async Task<ActionResult<Device>> PostDevice(Device device)
        {
            device.Status = DeviceStatus.Submitted;
            device.Origin = DeviceOrigin.User;
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
