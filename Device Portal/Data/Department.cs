using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public class Faculty 
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public HashSet<User> Users { get; set; }
    }
    public class Department
    {
        public int Id { get; set; }        
        public string Name { get; set; }

        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; }

        public int? ParentDepartmentId { get; set; }
        public Department ParentDepartment { get; set; }

        [JsonIgnore]
        public HashSet<Device> Devices { get; set; }
        [JsonIgnore]
        public HashSet<User_Department> Users { get; set; }
        [JsonIgnore]
        public HashSet<Labnet> Labnets { get; set; }
    }
}
