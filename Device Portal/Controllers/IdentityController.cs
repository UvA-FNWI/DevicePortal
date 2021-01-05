using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private OpenIdConnectHandler handler;
        public IdentityController(OpenIdConnectHandler handler) 
        {
            this.handler = handler;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Get()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray();
            return new JsonResult(claims);
        }
        
        [HttpGet("enter")]
        public IActionResult Enter()
        {
            return Redirect("~/"); // Redirect to root
        }

        [HttpGet("exit")]
        public async Task<IActionResult> Exit() 
        {            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
    }
}
