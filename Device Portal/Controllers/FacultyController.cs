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
            var userName = User.GetUserName();
            bool isAdmin = User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin);

            HashSet<int> departmentIds;
            if (isAdmin)
            {
                departmentIds = _context.Departments
                    .Select(d => d.Id)
                    .ToHashSet();
            }
            else
            {
                departmentIds = _context.Users_Departments
                    .Where(u => u.UserName == userName && u.CanManage)
                    .Select(u => u.DepartmentId)
                    .ToHashSet();
            }

            var departments = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    Devices = d.Devices.Where(dev => !string.IsNullOrEmpty(dev.UserName)),
                }).ToArrayAsync();

            var result = new List<Department>();
            foreach (var department in departments)
            {
                var users = department.Devices.GroupBy(d => d.UserName);                
                result.Add(new Department
                {
                    Id = department.Id,
                    Name = department.Name,
                    Devices = department.Devices.Count(),
                    Users = users.Count(),
                    UsersCompleted = users.Count(u => u.Any(dev => dev.Status == DeviceStatus.Approved)),
                });
            }

            return Ok(result);
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
