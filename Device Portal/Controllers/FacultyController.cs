using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;
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
    [ApiController, Authorize(Policy = AppPolicies.ManagerOnly)]
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
                .Where(u => u.Departments.Any())
                .Select(u => new
                {
                    Departments = u.Departments.Select(d => d.Department),
                    Completed = u.Devices.Any(d => d.Status == DeviceStatus.Approved),
                    Devices = u.Devices.Count,
                }).ToArrayAsync();

            var result = new Dictionary<int, Department>();
            foreach (var user in users)
            {
                foreach (var department in user.Departments) 
                {
                    if (!result.TryGetValue(department.Id, out Department dep))
                    {
                        dep = new Department
                        {
                            Id = department.Id,
                            Name = department.Name,
                            Devices = 0,
                            Users = 0,
                            UsersCompleted = 0,
                        };
                        result.Add(dep.Id, dep);
                    }
                    dep.Users++;
                    dep.Devices += user.Devices;
                    if (user.Completed) { ++dep.UsersCompleted; }
                }
            }
            return Ok(result.Values);
        }

        class Department 
        { 
            public int Id { get; set; }
            public string Name { get; set; }
            public int Users { get; set; }
            public int UsersCompleted { get; set; }
            public int Devices { get; set; }
        }
    }
}
