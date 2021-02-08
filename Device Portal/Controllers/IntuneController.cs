using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize(Policy = AppPolicies.AdminOnly)]
    public class IntuneController : ControllerBase
    {
        private readonly IntuneService _intuneService;
        private readonly PortalContext _context;

        public IntuneController(PortalContext context, IntuneService intuneService)
        {
            _context = context;
            _intuneService = intuneService;
        }

        // https://docs.microsoft.com/en-us/graph/api/intune-devices-manageddevice-list?view=graph-rest-1.0
        [HttpGet("managedDevices")]
        public async Task<ActionResult> GetManagedDevices()
        {
            return Ok(await _intuneService.GetManagedDevices());
        }

        // https://docs.microsoft.com/en-us/graph/api/intune-devices-manageddevice-list?view=graph-rest-1.0
        [HttpGet("managedDevices/{userId}")]
        public async Task<ActionResult> GetManagedDevices(string userId)
        {
            try
            {
                return Ok(await _intuneService.GetManagedDevicesUser(userId));
            }
            catch { return NotFound(); }
        }

        [HttpGet("managedDevices/{userName}/sync")]
        public async Task<ActionResult> GetManagedDevicesSyncUser(string userName)
        {
            var user = await _context.Users.FindAsync(userName);
            if (user == null) { return NotFound(); }

            await _intuneService.SyncManagedDevicecUser(user.UserName, user.ObjectId);
            return Ok();
        }

        [HttpGet("managedDevices/sync")]
        public async Task<ActionResult> GetManagedDevicesSync()
        {
            await _intuneService.SyncManagedDevices();
            return Ok();
        }

        [HttpGet("users/sync")]
        public async Task<ActionResult> GetUsersSync()
        {
            await _intuneService.SyncUsers();
            return Ok();
        }
    }
}
