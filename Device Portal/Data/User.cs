using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class User
    {
        public static readonly string ImporterId = "<CMDB importer>";
        public static readonly string IntuneServiceId = "<intune service>";
        public static readonly string LabnetId = "<Labnet importer>";

        public User() 
        {
            Departments = new HashSet<User_Department>();
        }

        [Key]
        public string UserName { get; set; }
        public string ObjectId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }

        public bool CanSecure { get; set; }
        public bool CanApprove { get; set; }
        public bool CanAdmin { get; set; }

        public bool Inactive { get; set; }
    
        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; }

        [JsonIgnore]
        public HashSet<User_Department> Departments { get; set; }
        public HashSet<SecurityCheck> SecurityChecks { get; set; }
        [JsonIgnore]
        public HashSet<Device> Devices { get; set; }
    }

    public class User_Department
    {
        public bool CanManage { get; set; }

        [ForeignKey("User")]
        public string UserName { get; set; }
        public User User { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; }
    }
}
