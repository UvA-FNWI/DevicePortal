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

namespace Device_Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntuneController : ControllerBase
    {
        private static ClientCredentialProvider authProvider;

        public IntuneController(IConfiguration configuration)
        {
            if (authProvider == null)             
            { 
                string clientId = configuration["AzureAD:ClientID"];
                string clientSecret = configuration["AzureAD:ClientSecret"];
                string tentantId = configuration["AzureAD:TenantID"]; ;

                var confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tentantId)
                    .WithClientSecret(clientSecret)
                    .Build();
                authProvider = new ClientCredentialProvider(confidentialClientApplication);
            }
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

        private static GraphServiceClient GetGraphClient() 
        {
            return new GraphServiceClient(authProvider);
        }
    }
}
