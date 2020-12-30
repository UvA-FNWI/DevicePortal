using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Faculty { get; set; }
        public string Institute { get; set; }

        public bool CanSecure { get; set; }
        public bool CanApprove { get; set; }
        public bool CanAdmin { get; set; }
    }
}
