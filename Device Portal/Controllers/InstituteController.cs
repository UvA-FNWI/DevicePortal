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
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices(string name)
        {
            return await db.Devices.Where(d => d.User.Institute == name).ToListAsync();
        }
    }
}
