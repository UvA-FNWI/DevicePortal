using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityChecksController : ControllerBase
    {
        private readonly PortalContext _context;

        public SecurityChecksController(PortalContext context)
        {
            _context = context;
        }

        // GET: api/SecurityChecks
        [HttpGet]
        [Authorize(Policy = AppPolicies.ApproverOnly)]
        public async Task<ActionResult<IEnumerable<SecurityCheck>>> GetSecurityChecks()
        {
            return await _context.SecurityChecks.ToListAsync();
        }

        // GET: api/SecurityChecks/Submitted
        [Authorize(Policy = AppPolicies.ApproverOnly)]
        [HttpGet("Submitted")]
        public async Task<ActionResult> GetSecurityChecksSubmitted()
        {
            var query = _context.SecurityChecks.AsQueryable();
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) ||
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                query = query.Where(c => c.Status == DeviceStatus.Submitted);
            }
            else
            {
                var departmentIds = User.GetDepartmentIds(_context);
                query = query.Where(c => c.Status == DeviceStatus.Submitted && departmentIds.Contains(c.Device.DepartmentId));
            }
            var checks = await query.Select(c => new
                {
                    c.Id,
                    c.UserName,
                    UserFullName = c.User.Name,
                    DeviceName = c.Device.Name,
                    c.Device.DeviceId,
                    DepartmentName = c.Device.Department.Name,
                })
                .ToListAsync();
            return Ok(checks);
        }

        // GET: api/SecurityChecks/Count
        [HttpGet("Count")]
        [Authorize(Policy = AppPolicies.ApproverOnly)]
        public async Task<ActionResult<int>> GetSecurityCheckCount()
        {
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) ||
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                return await _context.SecurityChecks.CountAsync();
            }

            var departmentIds = User.GetDepartmentIds(_context);
            return await _context.SecurityChecks
                .Where(c => departmentIds.Contains(c.Device.DepartmentId))
                .CountAsync();
        }

        // GET: api/SecurityChecks/Submitted/Count
        [HttpGet("Submitted/Count")]
        [Authorize(Policy = AppPolicies.ApproverOnly)]
        public async Task<ActionResult<int>> GetSecurityCheckCountSubmitted()
        {
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) ||
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                return await _context.SecurityChecks.Where(c => c.Status == DeviceStatus.Submitted).CountAsync();
            }

            var departmentIds = User.GetDepartmentIds(_context);
            return await _context.SecurityChecks
                .Where(c => c.Status == DeviceStatus.Submitted && departmentIds.Contains(c.Device.DepartmentId))
                .CountAsync();
        }

        [Authorize(Policy = AppPolicies.SecurityCheckAccess)]
        // GET: api/SecurityChecks/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetSecurityCheck(int id)
        {
            var securityCheck = await _context.SecurityChecks
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.UserName,
                    UserDisplayName = c.User.Name,
                    c.DeviceId,
                    c.Device,
                    c.SubmissionDate,
                    c.Status,
                    c.StatusEffectiveDate,
                    c.Questions,
                }).FirstOrDefaultAsync();

            string userId = User.GetUserName();
            if (!User.HasClaim(AppClaimTypes.Permission, AppClaims.CanApprove) &&
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanSecure) &&
                userId != securityCheck.UserName) 
            {
                return Forbid();
            }

            if (securityCheck == null)
            {
                return NotFound();
            }

            return Ok(securityCheck);
        }

        [Authorize(Policy = AppPolicies.SecurityCheckAccess)]
        // GET: api/SecurityCheck/Device/5
        [HttpGet("Device/{id}")]
        public async Task<ActionResult<SecurityCheck>> GetSecurityCheckDevice(int id)
        {
            var securityCheck = await _context.SecurityChecks
                .Include(c => c.Questions)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(c => c.DeviceId == id);

            if (securityCheck == null)
            {
                return NotFound();
            }

            return securityCheck;
        }

        public class SecurityCheckUpdate
        {
            public Device device { get; set; }
            public SecurityCheck check { get; set; }
        }

        [Authorize(Policy = AppPolicies.AuthorizedOnly)]
        // PUT: api/SecurityChecks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSecurityCheck(int id, SecurityCheckUpdate update)
        {
            if (id != update.check.Id)
            {
                return BadRequest();
            }
            if (!SecurityCheckExists(id))
            {
                return NotFound();
            }
            if (!_context.SecurityChecks.Any(c => c.Id == id && c.Status == DeviceStatus.Submitted))
            {
                return BadRequest("Expected security check status to be submitted.");
            }

            var device = await _context.Devices.FindAsync(update.check.DeviceId);
            if (device == null)
            {
                return NotFound();
            }
            if (device.UserName != User.GetUserName())
            {
                return Forbid();
            }

            DateTime now = DateTime.Now;
            foreach (var q in update.check.Questions)
            {
                _context.CreateOrUpdate(q);
            }
            update.check.Status = DeviceStatus.Submitted;
            update.check.StatusEffectiveDate = now;
            _context.Entry(update.check).State = EntityState.Modified;

            if (device.Status != update.check.Status ||
                device.OS_Type != update.device.OS_Type ||
                device.OS_Version != update.device.OS_Version)
            {
                _context.DeviceHistories.Add(new DeviceHistory(device));
                device.DateEdit = now;
                device.UserEditId = User.GetUserName();
                device.Status = update.check.Status;
                device.StatusEffectiveDate = now;
                device.OS_Type = update.device.OS_Type;
                device.OS_Version = update.device.OS_Version;
                _context.UpdateProperties(device, d => d.DateEdit, d => d.UserEditId, 
                    d => d.Status, d => d.StatusEffectiveDate, d => d.OS_Type, d => d.OS_Version);
            }
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Policy = AppPolicies.ApproverOnly)]
        // PUT: api/SecurityChecks/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> PutSecurityCheckStatus(int id, SecurityCheck securityCheck)
        {
            if (id != securityCheck.Id)
            {
                return BadRequest();
            }
            if (!SecurityCheckExists(id))
            {
                return NotFound();
            }
            if (!_context.SecurityChecks.Any(c => c.Id == id && c.Status == DeviceStatus.Submitted))
            {
                return BadRequest("Expected security check status to be submitted.");
            }
            
            var device = await _context.Devices.FindAsync(securityCheck.DeviceId);
            if (device == null)
            {
                return NotFound();
            }

            if (device.Status != securityCheck.Status)
            {
                DateTime now = DateTime.Now;
                _context.DeviceHistories.Add(new DeviceHistory(device));
                device.DateEdit = now;
                device.UserEditId = User.GetUserName();
                device.Status = securityCheck.Status;
                device.StatusEffectiveDate = securityCheck.StatusEffectiveDate = now;
                _context.UpdateProperties(device, d => d.DateEdit, d => d.UserEditId, 
                    d => d.Status, d => d.StatusEffectiveDate);
            }

            _context.UpdateProperties(securityCheck, c => c.Status, c => c.StatusEffectiveDate);            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Policy = AppPolicies.AuthorizedOnly)]
        // POST: api/SecurityChecks
        [HttpPost]
        public async Task<ActionResult<SecurityCheck>> PostSecurityCheck(SecurityCheckUpdate update)
        {
            var device = await _context.Devices.FindAsync(update.check.DeviceId);
            if (device == null)
            {
                return NotFound();
            }

            string userName = User.GetUserName();
            if (device.UserName != userName)
            {
                return Forbid();
            }

            await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () => { 
                var trans =_context.Database.BeginTransaction();

                DateTime now = DateTime.Now;
                update.check.SubmissionDate = now;
                update.check.Status = DeviceStatus.Submitted;
                update.check.StatusEffectiveDate = now;
                update.check.UserName = userName;
                _context.SecurityChecks.Add(update.check);

                if (device.Status != update.check.Status ||
                    device.OS_Type != update.device.OS_Type ||
                    device.OS_Version != update.device.OS_Version)
                {
                    _context.DeviceHistories.Add(new DeviceHistory(device));
                    device.DateEdit = now;
                    device.UserEditId = User.GetUserName();
                    device.Status = update.check.Status;
                    device.StatusEffectiveDate = now;
                    device.OS_Type = update.device.OS_Type;
                    device.OS_Version = update.device.OS_Version;
                    _context.UpdateProperties(device, d => d.DateEdit, d => d.UserEditId, 
                        d => d.Status, d => d.StatusEffectiveDate, d => d.OS_Type, d => d.OS_Version);
                }

                await _context.SaveChangesAsync();
                await trans.CommitAsync();
            });

            return CreatedAtAction("GetSecurityCheck", new { id = update.check.Id }, update.check);
        }

        // DELETE: api/SecurityChecks/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteSecurityCheck(int id)
        //{
        //    var securityCheck = await _context.SecurityChecks.FindAsync(id);
        //    if (securityCheck == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.SecurityChecks.Remove(securityCheck);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool SecurityCheckExists(int id)
        {
            return _context.SecurityChecks.Any(e => e.Id == id);
        }
    }
}
