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
    [ApiController, Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminController : ControllerBase
    {
        private NotificationTask _notificationTask;
        private RightsTask _rightsTask;

        public AdminController(NotificationTask notificationTask, RightsTask rightsTask) 
        {
            _notificationTask = notificationTask;
            _rightsTask = rightsTask;
        }

        [HttpGet("notify/approvers")]
        public ActionResult NotifyApprovers()         
        {
            _notificationTask.NotifyApprovers();
            return Ok();
        }

        [HttpGet("update/rights")]
        public async Task<ActionResult> UpdateRights()
        {
            await _rightsTask.Run();
            return Ok();
        }
    }
}
