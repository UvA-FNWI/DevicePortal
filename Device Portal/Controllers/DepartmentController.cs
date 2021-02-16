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
            bool isAdmin = User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin);
            if (!isAdmin && !db.Users_Departments.Any(u => u.UserName == userName && u.DepartmentId == id && u.CanManage))
            {
                return Forbid();
            }
            if (!db.Departments.Any(d => d.Id == id)) { return NotFound(); }
            var department = await db.Departments.FirstAsync(d => d.Id == id);

            var devices = await db.Devices
                .Where(d => d.DepartmentId == id && !string.IsNullOrEmpty(d.UserName))
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
                    d.Category,
                    User = d.User.Name,
                    d.UserName,
                    d.User.Email,
                })
                .ToArrayAsync();
            var users = await db.Users
               .Select(u => new
               {
                   u.UserName,
                   u.CanApprove,
                   u.CanSecure,
                   Departments = u.Departments.Select(d => d.DepartmentId).ToHashSet(),
               })
               .ToArrayAsync();
            var userNameSet = devices.Select(d => d.UserName).ToHashSet();

            users = users.Where(u => u.Departments.Contains(id) || userNameSet.Contains(u.UserName)).ToArray();
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
