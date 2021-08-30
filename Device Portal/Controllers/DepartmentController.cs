using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize(Policy = AppPolicies.ManagerOnly)]
    public class DepartmentController : ControllerBase
    {
        private readonly PortalContext db;

        public DepartmentController(PortalContext context)
        {
            db = context;
        }

        // GET: api/Department/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(int id) 
        {
            if (!db.Departments.Any(d => d.Id == id)) { return NotFound(); }
            return await db.Departments.FirstAsync(d => d.Id == id);
        }

        // GET: api/Department/{id}/Overview
        [HttpGet("{id}/Overview")]
        public async Task<ActionResult> GetDevices(int id)
        {
            var userName = User.GetUserName();            
            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) &&
                !User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty) &&
                !db.Users_Departments.Any(u => u.UserName == userName && u.DepartmentId == id && u.CanManage))
            {
                return Forbid();
            }
            if (!db.Departments.Any(d => d.Id == id)) { return NotFound(); }
            
            var departments = await db.Departments.Where(d => d.Id == id || d.ParentDepartmentId == id).ToListAsync();
            departments.Sort((a, b) => (b.ParentDepartmentId ?? 0) - (a.ParentDepartmentId ?? 0));
            var departmentIds = departments.Select(d => d.Id);

            var devices = await db.Devices
                .Include(d => d.User)
                .Where(d => departmentIds.Contains(d.DepartmentId)).Active()
                .ToArrayAsync();
            var userEditIds = devices.Where(d => d.UserEditId != null).Select(d => d.UserEditId).ToHashSet();
            var editNameMap = await db.Users
                .Where(u => userEditIds.Contains(u.UserName))
                .Select(u => new { u.UserName, u.Name })
                .ToDictionaryAsync(u => u.UserName, u => u.Name);
            foreach (var d in devices)
            {
                d.UserEditName = (d.UserEditId == null || !editNameMap.TryGetValue(d.UserEditId, out var name)) ? null : name;
            }

            var users = await db.Users
               .Select(u => new
               {
                   u.UserName,
                   u.CanApprove,
                   u.CanSecure,
                   Departments = u.Departments.Select(d => d.DepartmentId).ToHashSet(),
               })
               .ToArrayAsync();
            var userNameSet = devices.Where(d => !string.IsNullOrEmpty(d.UserName)).Select(d => d.UserName).ToHashSet();

            var department = departments.Last();
            users = users.Where(u => u.Departments.Overlaps(departmentIds) || userNameSet.Contains(u.UserName)).ToArray();
            return Ok(new 
            {
                Name = department.Name,
                Devices = devices,
                Users = users.Length,
                UsersAuthorized = users.Count(u => u.CanSecure),
                UsersApprover = users.Count(u => u.CanApprove),
            });
        }
    }
}
