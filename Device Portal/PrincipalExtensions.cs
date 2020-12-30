using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevicePortal
{
    public static class PrincipalExtensions
    {
        public static string GetUserName(this ClaimsPrincipal claimsPrincipal) 
        {
            return claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "uids")?.Value;
        }
    }
}
