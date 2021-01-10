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
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin))
            {
                return await _context.Users.ToListAsync();
            }
            else 
            {
                var userId = User.GetUserName();
                string department = _context.Users
                    .Where(u => u.UserName == userId)
                    .Select(u => u.Department)
                    .FirstOrDefault();
                return await _context.Users.Where(u => u.Department == department).ToListAsync();
            }
        }
                
        // GET: api/Users/{UserName}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }
            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin))
            {
                string userId = User.GetUserName();
                string department = _context.Users
                    .Where(u => u.UserName == userId)
                    .Select(u => u.Department)
                    .FirstOrDefault();
                if (user.Department != department) { return Forbid(); }
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

            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin))
            {
                string userId = User.GetUserName();
                string department = _context.Users
                    .Where(u => u.UserName == userId)
                    .Select(u => u.Department)
                    .FirstOrDefault();
                if (user.Department != department) { return Forbid(); }
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
