using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
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
    [ApiController, Authorize(Policy = AppPolicies.AdminOnly)]
    public class IntuneController : ControllerBase
    {
        public static ClientCredentialProvider authProvider;

        private readonly PortalContext _context;

        public IntuneController(PortalContext context) 
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<ActionResult> GetMe() 
        {
            var graphClient = GetGraphClient();
            var me = await graphClient.Me.Request().GetAsync();
            return Ok(me);
        }

        // https://docs.microsoft.com/en-us/graph/api/device-list?view=graph-rest-1.0&tabs=http
        [HttpGet("devices")]
        public async Task<ActionResult> GetDevices()
        {
            var graphClient = GetGraphClient();
            var devices = await graphClient.Devices.Request().GetAsync();
            return Ok(devices);
        }

        // https://docs.microsoft.com/en-us/graph/api/resources/intune-devices-manageddeviceoverview?view=graph-rest-1.0
        [HttpGet("overview")]
        public async Task<ActionResult> GetOverview()
        {
            var graphClient = GetGraphClient();
            var devices = await graphClient.DeviceManagement.ManagedDeviceOverview.Request().GetAsync();
            return Ok(devices);
        }

        // https://docs.microsoft.com/en-us/graph/api/intune-devices-manageddevice-list?view=graph-rest-1.0
        [HttpGet("managedDevices")]
        public async Task<ActionResult> GetManagedDevices() 
        {
            var graphClient = GetGraphClient();
            var devices = await graphClient.DeviceManagement.ManagedDevices.Request().GetAsync();
            return Ok(devices);
        }

        // https://docs.microsoft.com/en-us/graph/api/intune-devices-manageddevice-list?view=graph-rest-1.0
        [HttpGet("managedDevices/{userId}")]
        public async Task<ActionResult> GetManagedDevices(string userId)
        {
            var graphClient = GetGraphClient();
            var devices = await graphClient.Users[userId].ManagedDevices.Request().GetAsync();
            return Ok(devices);
        }            

        [HttpGet("users/sync")]
        public async Task<ActionResult> GetUsers()
        {
            var graphClient = GetGraphClient();
            var users = await _context.Users.ToListAsync();
            foreach (var user in users) 
            {
                if (string.IsNullOrEmpty(user.Email)) { continue; }

                var info = await graphClient.Users.Request()
                    .Filter($"userPrincipalName eq '{user.Email}'")
                    .Select("displayName, id")                
                    .GetAsync();

                if (info.Any()) 
                {
                    user.ObjectId = info[0].Id;
                    user.Name = info[0].DisplayName;

                    var entry = _context.Entry(user);
                    entry.Property(u => u.ObjectId).IsModified = true;
                    entry.Property(u => u.Name).IsModified = true;
                }
            }
            await _context.SaveChangesAsync();

            return Ok();
        }

        private static GraphServiceClient GetGraphClient() 
        {
            return new GraphServiceClient(authProvider);
        }
    }
}
