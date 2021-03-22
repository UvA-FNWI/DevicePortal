using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Text.Json;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize(Policy = AppPolicies.ManagerOnly)]
    public class UsersController : ControllerBase
    {
        private readonly PortalContext _context;
        private readonly DepartmentService _departmentService;

        public UsersController(PortalContext context, DepartmentService departmentService)
        {
            _context = context;
            _departmentService = departmentService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult> GetUsers()
        {
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) ||
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                return Ok(await _context.Users
                    .Select(u => new 
                    {
                        u.CanApprove,
                        u.CanSecure,
                        Institute = string.Join(',', u.Departments.Select(d => d.Department.Name)),
                        u.Email,
                        u.FacultyId,
                        u.Name,
                        u.UserName,
                    })                    
                    .ToListAsync());
            }
            else 
            {
                var departmentIds = User.GetDepartmentIds(_context);
                return Ok(await _context.Users
                    .Where(u => u.Departments.Any(d => departmentIds.Contains(d.DepartmentId)))
                    .Select(u => new
                    {
                        u.CanApprove,
                        u.CanSecure,
                        Institute = string.Join(',', u.Departments.Select(d => d.Department.Name)),
                        u.Email,
                        u.FacultyId,
                        u.Name,
                        u.UserName,
                    })
                    .ToArrayAsync());
                    
            }
        }
                
        // GET: api/Users/{UserName}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _context.Users
                .Include(u => u.Departments)
                .FirstOrDefaultAsync(u => u.UserName == id);

            if (user == null)
            {
                return NotFound();
            }
            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) &&
                !User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                var departmentIds = User.GetDepartmentIds(_context);
                if (!user.Departments.Any(d => departmentIds.Contains(d.DepartmentId))) { return Forbid(); }
            }

            return user;
        }

        [Authorize(Policy = AppPolicies.AdminOnly)]
        // GET: api/Users/{UserName}/Rights
        [HttpGet("{id}/Rights")]
        public async Task<ActionResult<DepartmentService.Department[]>> GetUserRights(string id)
        {
            return await _departmentService.GetDepartments(id);
        }

        // PUT: api/Users/{UserName}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(string id, User user)
        {
            if (id != user.UserName)
            {
                return BadRequest();
            }

            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) &&
                !User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                string userName = User.GetUserName();
                var departmentIds = _context.Users_Departments
                    .Where(u => u.UserName == userName && u.CanManage)
                    .Select(u => u.DepartmentId)
                    .ToHashSet();
                bool canManageDepartment = _context.Users_Departments
                    .Any(ud => ud.UserName == user.UserName && departmentIds.Contains(ud.DepartmentId));
                if (!canManageDepartment) { return Forbid(); }
            }
            
            var entry = _context.Entry(user);
            entry.Property(u => u.CanApprove).IsModified = true;
            entry.Property(u => u.CanSecure).IsModified = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(u => u.UserName == id))
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
    }
}
