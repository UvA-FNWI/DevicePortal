using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public enum DeviceOwner
    {
        Person,
        Group,
        Location
    }

    public enum DeviceType
    {
        Mobile = 1 << 0,
        Tablet = 1 << 1,
        Laptop = 1 << 2,
        Desktop = 1 << 3,
        All = (1 << 4) - 1,
    }

    public enum DeviceOrigin
    { 
        DataExport,
        Intune,
        User,
    }

    public enum DeviceStatus
    {
        Approved,
        Denied,
        Submitted,
        Unsecure,
    }

    public class Device : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string SerialNumber { get; set; }
        [JsonPropertyName("os")]
        public string OS { get; set; }
        public DeviceType Type { get; set; }
        public DeviceStatus Status { get; set; }
        public DateTime StatusEffectiveDate { get; set; }
        public DeviceOrigin Origin { get; set; }

        [ForeignKey("User")]
        public string UserName { get; set; }
        public User User { get; set; }

        [JsonIgnore]
        public HashSet<SecurityCheck> SecurityChecks { get; set; }
    }
}
