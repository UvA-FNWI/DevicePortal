using System;
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
    public class SecurityQuestionsController : ControllerBase
    {
        private readonly PortalContext _context;

        public SecurityQuestionsController(PortalContext context)
        {
            _context = context;
        }

        // GET: api/SecurityQuestions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SecurityQuestions>>> GetSecurityQuestions()
        {
            return await _context.SecurityQuestions.ToListAsync();
        }

        // GET: api/SecurityQuestions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SecurityQuestions>> GetSecurityQuestions(int id)
        {
            var securityQuestions = await _context.SecurityQuestions.FindAsync(id);

            if (securityQuestions == null)
            {
                return NotFound();
            }

            return securityQuestions;
        }

        // PUT: api/SecurityQuestions/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutSecurityQuestions(int id, SecurityQuestions securityQuestions)
        //{
        //    if (id != securityQuestions.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(securityQuestions).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!SecurityQuestionsExists(id))
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

        // POST: api/SecurityQuestions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<SecurityQuestions>> PostSecurityQuestions(SecurityQuestions securityQuestions)
        //{
        //    _context.SecurityQuestions.Add(securityQuestions);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetSecurityQuestions", new { id = securityQuestions.Id }, securityQuestions);
        //}

        // DELETE: api/SecurityQuestions/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteSecurityQuestions(int id)
        //{
        //    var securityQuestions = await _context.SecurityQuestions.FindAsync(id);
        //    if (securityQuestions == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.SecurityQuestions.Remove(securityQuestions);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool SecurityQuestionsExists(int id)
        {
            return _context.SecurityQuestions.Any(e => e.Id == id);
        }
    }
}
