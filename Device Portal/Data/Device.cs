using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Device_Portal.Data
{
    public enum DeviceOwner 
    {
        Person,
        Group,
        Location
    }

    public class Device
    {
        public int Id { get; set; }
        public string Faculty { get; set; }
        public string Institute { get; set; }
    }
}
