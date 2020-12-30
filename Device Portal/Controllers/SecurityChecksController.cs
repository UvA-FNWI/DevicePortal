﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevicePortal.Data;

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
        public async Task<ActionResult<IEnumerable<SecurityCheck>>> GetSecurityChecks()
        {
            return await _context.SecurityChecks.Include(c => c.Questions).ToListAsync();
        }

        // GET: api/SecurityChecks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SecurityCheck>> GetSecurityCheck(int id)
        {
            var securityCheck = await _context.SecurityChecks
                .Include(c => c.Questions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (securityCheck == null)
            {
                return NotFound();
            }

            return securityCheck;
        }

        // PUT: api/SecurityChecks/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutSecurityCheck(int id, SecurityCheck securityCheck)
        //{
        //    if (id != securityCheck.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(securityCheck).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!SecurityCheckExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/SecurityChecks
        [HttpPost]
        public async Task<ActionResult<SecurityCheck>> PostSecurityCheck(SecurityCheck securityCheck)
        {
            var device = await _context.Devices.FindAsync(securityCheck.DeviceId);
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
                device.Status = DeviceStatus.Submitted;
                _context.UpdateProperties(device, d => d.Status);

                securityCheck.SubmissionDate = DateTime.Now;
                securityCheck.UserName = userName;
                _context.SecurityChecks.Add(securityCheck);

                await _context.SaveChangesAsync();
                await trans.CommitAsync();
            });

            return CreatedAtAction("GetSecurityCheck", new { id = securityCheck.Id }, securityCheck);
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
