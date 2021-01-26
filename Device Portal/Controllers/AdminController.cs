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
        private NotificationService _notificationService;

        public AdminController(NotificationService notificationService) 
        {
            _notificationService = notificationService;
        }

        [HttpGet("notify/approvers")]
        public ActionResult NotifyApprovers()         
        {
            _notificationService.NotifyApprovers();
            return Ok();
        }
    }
}
