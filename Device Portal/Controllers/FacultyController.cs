using DevicePortal.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultiesController : ControllerBase
    {
        private readonly PortalContext _context;

        public FacultiesController(PortalContext context)
        {
            _context = context;
        }

        // GET: api/Faculties
        [HttpGet]
        public async Task<ActionResult> GetOverview()
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Institute))
                .Select(u => new
                {
                    u.Institute,
                    Completed = u.Devices.Any(d => d.Status == DeviceStatus.Approved),
                    Devices = u.Devices.Count,
                }).ToArrayAsync();

            var result = new Dictionary<string, Institute>();
            foreach (var user in users)
            {
                if (!result.TryGetValue(user.Institute, out Institute inst))
                {
                    inst = new Institute
                    {
                        Name = user.Institute,
                        Devices = 0,
                        Users = 0,
                        UsersCompleted = 0,
                    };
                    result.Add(inst.Name, inst);
                }
                inst.Users++;
                inst.Devices += user.Devices;
                if (user.Completed) { ++inst.UsersCompleted; }
            }
            return Ok(result.Values);
        }

        class Institute 
        { 
            public string Name { get; set; }
            public int Users { get; set; }
            public int UsersCompleted { get; set; }
            public int Devices { get; set; }
        }
    }
}
