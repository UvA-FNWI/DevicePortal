﻿using System;
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

        public static string GetFullName(this ClaimsPrincipal claimsPrincipal)
        {
            string giveName = "";
            string familyName = "";
            foreach (var claim in claimsPrincipal.Claims) 
            {
                if (claim.Type == "give_name") { giveName = claim.Value; }
                else if (claim.Type == "family_name") { familyName = claim.Value; }
            }
            return $"{giveName} {familyName}".Trim();
        }
    }
}
