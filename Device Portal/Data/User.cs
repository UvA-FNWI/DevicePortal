using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class User
    {
        [Key]
        public string UserName { get; set; }
        public string ObjectId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Faculty { get; set; }
        public string Institute { get; set; }
        public string Department { get; set; }

        public bool CanSecure { get; set; }
        public bool CanApprove { get; set; }
        public bool CanManage { get; set; }
        public bool CanAdmin { get; set; }

        public HashSet<SecurityCheck> SecurityChecks { get; set; }
        public HashSet<Device> Devices { get; set; }
    }
}
