using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstituteController : ControllerBase
    {
        private readonly PortalContext db;

        public InstituteController(PortalContext context)
        {
            db = context;
        }

        // GET: api/Institute/{name}/Devices
        [HttpGet("{name}/Devices")]
        public async Task<ActionResult> GetDevices(string name)
        {
            var devices = await db.Devices
                .Where(d => d.User.Institute == name)
                .Select(d => new 
                {
                    d.DeviceId,
                    d.Id,
                    d.Name,
                    d.Origin,
                    os_type = d.OS_Type,
                    os_version = d.OS_Version,
                    d.SerialNumber,
                    d.Status,
                    d.StatusEffectiveDate,
                    d.Type,
                    User = d.User.Name,
                    d.UserName                    
                })
                .ToListAsync();
            return Ok(devices);
        }
    }
}
