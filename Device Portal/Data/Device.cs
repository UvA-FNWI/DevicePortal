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
    public enum DeviceCategory 
    {
        ManagedStandard = 1 << 0,
        ManagedSpecial = 1 << 1,
        SelfSupport = 1 << 2,
        Other = 1 << 3,
        BYOD = 1 << 4, // bring your own device 
        All = (1 << 5) - 1,
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
        Lost,
        Disposed,
    }

    public enum OS_Type 
    {
        Android = 1 << 0,
        iOS = 1 << 1,
        Linux = 1 << 2,
        MacOS = 1 << 3,
        Windows = 1 << 4,
        All = (1 << 5) - 1,
    }

    public class DeviceBase : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string SerialNumber { get; set; }
        [JsonPropertyName("os_type")]
        public OS_Type OS_Type { get; set; }
        [JsonPropertyName("os_version")]
        public string  OS_Version { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string CostCentre { get; set; }
        public DateTime? LastSeenDate { get; set; }
        public string ItracsBuilding { get; set; }
        public string ItracsRoom { get; set; }
        public string ItracsOutlet { get; set; }
        public string Macadres { get; set; }
        public string Notes { get; set; }
        
        public DeviceType Type { get; set; }
        public DeviceCategory Category { get; set; }
        public DeviceStatus Status { get; set; }
        public DateTime StatusEffectiveDate { get; set; }
        public DeviceOrigin Origin { get; set; }

        [ForeignKey("User")]
        public string UserName { get; set; }
        public User User { get; set; }

        public DateTime DateEdit { get; set; }
        public string UserEditId { get; set; }
        [NotMapped]
        public string UserEditName { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        public bool Disowned { get; set; }
        public bool Shared { get; set; }
    }

    public class Device : DeviceBase
    {
        [JsonIgnore]
        public HashSet<SecurityCheck> SecurityChecks { get; set; }
        [JsonIgnore]
        public HashSet<DeviceHistory> History { get; set; }
    }

    public class DeviceHistory : DeviceBase
    {
        public int OriginalDeviceId { get; set; }
        public Device OriginalDevice { get; set; }

        public DateTime DateHistory { get; set; }

        public DeviceHistory() { }

        public DeviceHistory(DeviceBase d)
        {
            OriginalDeviceId = d.Id;
            DateHistory = DateTime.Now;

            var type = typeof(DeviceBase);
            var properties = type.GetProperties();
            foreach (var p in properties)
            {
                p.SetValue(this, p.GetValue(d));
            }
            Id = 0;

            if (string.IsNullOrEmpty(UserName)) { UserName = null; }
        }
    }
}
