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
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserName,
                    u.CanApprove,
                    u.CanSecure,
                    Departments = u.Departments.Select(d => d.DepartmentId).ToHashSet(),
                })
                .ToArrayAsync();
            var usersMap = users.ToDictionary(u => u.UserName);
            var userNameSet = new HashSet<string>();

            var departmentStats = new List<Department>();
            foreach (var department in departments)
            {
                int devicesIntuneCompleted = 0, devicesCheckSubmitted = 0, devicesCheckApproved = 0;
                foreach (var d in department.Devices) 
                {
                    if (d.Status == DeviceStatus.Approved)
                    {
                        if (d.Origin == DeviceOrigin.Intune) { ++devicesIntuneCompleted; }
                        else { ++devicesCheckApproved; }
                    }
                    else if (d.Origin != DeviceOrigin.Intune && d.Status == DeviceStatus.Submitted)
                    {
                        ++devicesCheckSubmitted;
                    }                    
                }

                var depByUser = department.Devices.GroupBy(d => d.UserName);
                int usersIntuneCompleted = 0, usersCheckSubmitted = 0, usersCheckApproved = 0;
                foreach (var u in depByUser)
                {
                    userNameSet.Add(u.Key);

                    bool submitted = false;
                    foreach (var d in u.AsEnumerable())
                    {
                        if (d.Status == DeviceStatus.Approved)
                        {
                            if (d.Origin == DeviceOrigin.Intune) { ++usersIntuneCompleted; }
                            else { ++usersCheckApproved; }
                            break;
                        }
                        else if (d.Origin != DeviceOrigin.Intune)
                        {
                            submitted = submitted || d.Status == DeviceStatus.Submitted;
                        }                        
                    }
                    if (submitted) { ++usersCheckSubmitted; }
                }

                departmentStats.Add(new Department
                {
                    Id = department.Id,
                    Name = department.Name,
                    Devices = department.Devices.Count(),
                    DevicesBYOD = department.Devices.Count(d => d.Category == DeviceCategory.BYOD),
                    DevicesManaged = department.Devices.Count(d => d.Category == DeviceCategory.ManagedStandard || d.Category == DeviceCategory.ManagedSpecial),
                    DevicesSelfSupport = department.Devices.Count(d => d.Category == DeviceCategory.SelfSupport),
                    DevicesCheckApproved = devicesCheckApproved,
                    DevicesCheckSubmitted = devicesCheckSubmitted,
                    DevicesIntuneCompleted = devicesIntuneCompleted,
                    Users = depByUser.Count(),
                    UsersAuthorized = depByUser.Count(u => usersMap[u.Key].CanSecure),
                    UsersApprover = depByUser.Count(u => usersMap[u.Key].CanApprove),
                    UsersCheckApproved = usersCheckApproved,
                    UsersCheckSubmitted = usersCheckSubmitted,
                    UsersIntuneCompleted = usersIntuneCompleted,
                });
            }

            // Filter users based on device presence in department, User_Department is incomplete
            users = users.Where(u => u.Departments.Overlaps(departmentIds) || userNameSet.Contains(u.UserName)).ToArray();
            return Ok(new 
            {
                Departments = departmentStats,
                Users = users.Length,
                UsersAuthorized = users.Count(u => u.CanSecure),
                UsersApprover = users.Count(u => u.CanApprove),
            });
        }

        class Department
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Users { get; set; }
            public int UsersAuthorized { get; set; }
            public int UsersApprover { get; set; }
            public int UsersIntuneCompleted { get; set; }
            public int UsersCheckSubmitted { get; set; }
            public int UsersCheckApproved { get; set; }
            public int Devices { get; set; }
            public int DevicesManaged { get; set; }
            public int DevicesSelfSupport { get; set; }
            public int DevicesBYOD { get; set; }
            public int DevicesIntuneCompleted { get; set; }
            public int DevicesCheckSubmitted { get; set; }
            public int DevicesCheckApproved { get; set; }
        }
    }
}
