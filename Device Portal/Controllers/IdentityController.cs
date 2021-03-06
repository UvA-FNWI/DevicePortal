﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        [HttpGet("enter"), AllowAnonymous]
        public IActionResult Enter()
        {
            return new ChallengeResult(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    RedirectUri = Url.Page("/Index")
                });
        }

        [HttpGet("exit")]
        public async Task<IActionResult> Exit()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpGet("impersonate/{userId}"), Authorize(AppPolicies.AdminOnly)]
        public ActionResult Impersonate(string userId) 
        {
            CookieOptions option = new CookieOptions()
            {
                Secure = true,
                HttpOnly = true,
                Expires = DateTime.Now.AddMinutes(15)
            };            
            Response.Cookies.Append("DevicePortal_Impersonate", userId, option);

            return Ok();
        }

        [HttpGet("impersonate/end")]
        public ActionResult ImpersonateEnd()
        {
            Response.Cookies.Delete("DevicePortal_Impersonate");
            return Ok();
        }
    }
}
