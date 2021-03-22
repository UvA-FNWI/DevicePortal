﻿using System;
using System.Collections.Generic;
using System.Linq;
using DevicePortal.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DevicePortal.Importer
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();
            string connectionString = config.GetConnectionString("DefaultConnection");
            var options = new DbContextOptionsBuilder<PortalContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            using var portalContext = new PortalContext(options);
            using var dwhContext = new DWHPMContext();

            DateTime now = DateTime.Now;
            Faculty faculty = portalContext.Faculties.FirstOrDefault();
            Dictionary<string, Department> departmentMap = portalContext.Departments.ToDictionary(d => d.Name);
            Dictionary<string, User> userMap = portalContext.Users.ToDictionary(u => u.UserName);
            List<User> usersToAdd = new List<User>();
            List<User> usersToUpdate = new List<User>();
            Dictionary<(string username, string deviceId), Device> deviceMap = portalContext.Devices.ToArray().ToDictionary(d => (d.UserName, d.DeviceId));
            List<Device> devicesToAdd = new List<Device>();
            List<Device> devicesToUpdate = new List<Device>();
            List<Department> departmentsToAdd = new List<Department>();

            var devices = dwhContext.FnwiPortals.ToArray();
            foreach (var d in devices)
            {
                string departmentName = instituteDepartmentMap.TryGetValue(d.Klantorganisatie, out departmentName) ?
                           departmentName : d.Klantorganisatie;
                if (!departmentMap.TryGetValue(departmentName, out Department department))
                {
                    department = new Department { Name = departmentName, FacultyId = faculty.Id };
                    departmentMap.Add(departmentName, department);
                    departmentsToAdd.Add(department);
                }

                var device = new Device
                {
                    Name = $"{d.Merk} {d.Type}".Trim(),
                    DeviceId = d.Naam,
                    UserName = d.LoginGebruiker,
                    SerialNumber = d.Serienummer,
                    Origin = DeviceOrigin.DataExport,
                    Status = DeviceStatus.Unsecure,
                    StatusEffectiveDate = now,
                    Department = department,
                    CostCentre = d.Kostenplaats,
                    ItracsBuilding = d.ItracsGebouw,
                    ItracsOutlet = d.ItracsOutlet,
                    ItracsRoom = d.ItracsRuimte,
                    LastSeenDate = d.DatumLaatstGezien,
                    Macadres = d.Macadres,
                    Notes = d.NotitiesKlant,
                    PurchaseDate = d.Aanschafdatum,
                };

                string deviceType = d.Soort;
                if (deviceType.StartsWith("Desktop")) { device.Type = DeviceType.Desktop; }
                else if (deviceType.StartsWith("Laptop")) { device.Type = DeviceType.Laptop; }
                else if (deviceType.StartsWith("Tablet")) { device.Type = DeviceType.Tablet; }
                else if (deviceType.StartsWith("Mobiel")) { device.Type = DeviceType.Mobile; }

                if (deviceType.Contains("UvA Standaard SCCM")) { device.Category = DeviceCategory.ManagedStandard; }
                else if (deviceType.Contains("UvA Spec. SCCM")) { device.Category = DeviceCategory.ManagedSpecial; }
                else if (deviceType.Contains("UvA Zelfsupp.")) { device.Category = DeviceCategory.SelfSupport; }
                else { device.Category = DeviceCategory.Other; }

                string deviceOs = d.Besturingssysteem;
                foreach (string prefix in osTypeMap.Keys)
                {
                    if (deviceOs.StartsWith(prefix))
                    {
                        device.OS_Type = osTypeMap[prefix];
                        device.OS_Version = deviceOs[prefix.Length..];
                    }
                }
                if (device.OS_Type == 0 && device.Type == DeviceType.Tablet)
                {
                    device.OS_Type = d.Merk.Contains("Apple") ? OS_Type.iOS : OS_Type.Android;
                }

                if (!string.IsNullOrEmpty(device.UserName))
                {
                    if (userMap.TryGetValue(device.UserName, out var user))
                    {
                        if (user.Email != d.Email)
                        {
                            user.Email = d.Email;
                            usersToUpdate.Add(user);
                        }
                    }
                    else
                    {
                        user = new User()
                        {
                            UserName = device.UserName,
                            FacultyId = faculty.Id,
                            Departments = new HashSet<User_Department>() { new User_Department { Department = department } },
                            Email = d.Email,
                        };
                        usersToAdd.Add(user);
                        userMap.Add(user.UserName, user);
                    }

                    if (deviceMap.TryGetValue((device.UserName, device.DeviceId), out Device existing))
                    {
                        if (existing.UserName != device.UserName ||
                            existing.SerialNumber != device.SerialNumber ||
                            existing.Category != device.Category ||
                            existing.CostCentre != device.CostCentre ||
                            existing.DepartmentId != device.Department.Id || 
                            existing.ItracsBuilding != device.ItracsBuilding ||
                            existing.ItracsOutlet != device.ItracsOutlet ||
                            existing.ItracsRoom != device.ItracsRoom ||
                            existing.LastSeenDate != device.LastSeenDate ||
                            existing.Macadres != device.Macadres ||
                            existing.Name !=  device.Name ||
                            existing.PurchaseDate != device.PurchaseDate ||
                            existing.Notes != device.Notes)
                        {
                            existing.UserName = device.UserName;
                            existing.SerialNumber = device.SerialNumber;
                            existing.Category = device.Category;
                            existing.CostCentre = device.CostCentre;
                            existing.ItracsBuilding = device.ItracsBuilding;
                            existing.ItracsOutlet = device.ItracsOutlet;
                            existing.ItracsRoom = device.ItracsRoom;
                            existing.LastSeenDate = device.LastSeenDate;
                            existing.Macadres = device.Macadres;
                            existing.Name = device.Name;
                            existing.PurchaseDate = device.PurchaseDate;
                            existing.Notes = device.Notes;
                            if (device.Department != null) 
                            {
                                existing.DepartmentId = device.DepartmentId;
                            }
                            devicesToUpdate.Add(existing);
                        }
                    }
                    else { devicesToAdd.Add(device); }
                }
                else { devicesToAdd.Add(device); }
            }
            if (usersToUpdate.Any())
            {
                portalContext.Users.UpdateRange(usersToUpdate);
                portalContext.SaveChanges();
            }
            if (devicesToUpdate.Any())
            {
                portalContext.Devices.UpdateRange(devicesToUpdate);
                portalContext.SaveChanges();
            }
            if (departmentsToAdd.Any())
            {
                portalContext.AddRange(departmentsToAdd);
                portalContext.SaveChanges();
            }

            // https://www.michalbialecki.com/2020/05/03/entity-framework-core-5-vs-sqlbulkcopy-2/
            var connection = portalContext.Database.GetDbConnection() as SqlConnection;
            connection.Open();
            using var sqlBulk = new SqlBulkCopy(portalContext.Database.GetDbConnection() as SqlConnection);

            // Bulk insert users
            var userTable = new System.Data.DataTable();
            userTable.Columns.Add("UserName");
            userTable.Columns.Add("Email");
            userTable.Columns.Add("Name");
            userTable.Columns.Add("FacultyId");
            userTable.Columns.Add("CanSecure");
            userTable.Columns.Add("CanApprove");
            userTable.Columns.Add("CanAdmin");
            foreach (var user in usersToAdd)
            {
                userTable.Rows.Add(
                    user.UserName,
                    user.Email,
                    user.Name,
                    user.FacultyId,
                    user.CanSecure,
                    user.CanApprove,
                    user.CanAdmin);
            }
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("UserName", "UserName"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Email", "Email"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Name", "Name"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("FacultyId", "FacultyId"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanSecure", "CanSecure"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanApprove", "CanApprove"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanAdmin", "CanAdmin"));
            sqlBulk.DestinationTableName = "dbo.Users";
            sqlBulk.WriteToServer(userTable);

            // Bulk insert User_Department
            var udTable = new System.Data.DataTable();
            udTable.Columns.Add("UserName");
            udTable.Columns.Add("DepartmentId");
            udTable.Columns.Add("CanManage");
            foreach (var user in usersToAdd)
            {
                foreach (var dep in user.Departments)
                {
                    udTable.Rows.Add(user.UserName, dep.Department.Id, dep.CanManage);
                }
            }
            sqlBulk.ColumnMappings.Clear();
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("UserName", "UserName"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("DepartmentId", "DepartmentId"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanManage", "CanManage"));
            sqlBulk.DestinationTableName = "dbo.Users_Departments";
            sqlBulk.WriteToServer(udTable);

            // Bulk insert devices
            var deviceTable = new System.Data.DataTable();
            deviceTable.Columns.Add("UserName");
            deviceTable.Columns.Add("Name");
            deviceTable.Columns.Add("DeviceId");
            deviceTable.Columns.Add("SerialNumber");
            deviceTable.Columns.Add("OS_Type", typeof(int));
            deviceTable.Columns.Add("OS_Version");
            deviceTable.Columns.Add("Type", typeof(int));
            deviceTable.Columns.Add("Category", typeof(int));
            deviceTable.Columns.Add("Status", typeof(int));
            deviceTable.Columns.Add("StatusEffectiveDate");
            deviceTable.Columns.Add("Origin", typeof(int));
            deviceTable.Columns.Add("DepartmentId", typeof(int));
            deviceTable.Columns.Add("PurchaseDate");
            deviceTable.Columns.Add("CostCentre");
            deviceTable.Columns.Add("LastSeenDate");
            deviceTable.Columns.Add("ItracsBuilding");
            deviceTable.Columns.Add("ItracsRoom");
            deviceTable.Columns.Add("ItracsOutlet");
            deviceTable.Columns.Add("Macadres");
            deviceTable.Columns.Add("Notes");

            foreach (var device in devicesToAdd)
            {
                deviceTable.Rows.Add(
                    device.UserName,
                    device.Name,
                    device.DeviceId,
                    device.SerialNumber,
                    device.OS_Type,
                    device.OS_Version,
                    device.Type,
                    device.Category,
                    device.Status,
                    device.StatusEffectiveDate,
                    device.Origin,
                    device.Department.Id,
                    device.PurchaseDate,
                    device.CostCentre,
                    device.LastSeenDate,
                    device.ItracsBuilding,
                    device.ItracsRoom,
                    device.ItracsOutlet,
                    device.Macadres,
                    device.Notes);
            }

            sqlBulk.ColumnMappings.Clear();
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("UserName", "UserName"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Name", "Name"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("DeviceId", "DeviceId"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("SerialNumber", "SerialNumber"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("OS_Type", "OS_Type"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("OS_Version", "OS_Version"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Type", "Type"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Category", "Category"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Status", "Status"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("StatusEffectiveDate", "StatusEffectiveDate"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Origin", "Origin"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("DepartmentId", "DepartmentId"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("PurchaseDate", "PurchaseDate"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CostCentre", "CostCentre"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("LastSeenDate", "LastSeenDate"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("ItracsBuilding", "ItracsBuilding"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("ItracsRoom", "ItracsRoom"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("ItracsOutlet", "ItracsOutlet"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Macadres", "Macadres"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Notes", "Notes"));
            sqlBulk.DestinationTableName = "dbo.Devices";
            sqlBulk.WriteToServer(deviceTable);
            sqlBulk.Close();
            connection.Close();
        }

        static readonly Dictionary<string, OS_Type> osTypeMap = new Dictionary<string, OS_Type>()
        {
            { "Android", OS_Type.Android },
            { "iOS", OS_Type.iOS },
            { "macOS", OS_Type.MacOS },
            { "OS X", OS_Type.MacOS },
            { "Win 10 ", OS_Type.Windows },
            { "Win 7", OS_Type.Windows },
            { "Windows", OS_Type.Windows },
        };
        static readonly Dictionary<string, string> instituteDepartmentMap = new Dictionary<string, string>()
        {
            { "UvA/FNWI/Secretariaat FNWI", "FB" },
            { "UvA/FNWI/ICT-voorzieningen FNWI", "FB" },
            { "UvA/FNWI/Inst. voor Interdisciplinaire Studies", "IIS" },
            { "UvA/FNWI/College of Life Sciences", "CoLS" },
            { "UvA/FNWI/IBED", "IBED" },
            { "UvA/FNWI/Personeelszaken FNWI", "FB" },
            { "UvA/FNWI/Projectmanagement FNWI", "FB" },
            { "UvA/FNWI/KDV", "KDV" },
            { "UvA/FNWI/Staf overig FNWI", "FB" },
            { "UvA/FNWI/ILLC", "ILLC" },
            { "UvA/FNWI/IoP", "IoP" },
            { "UvA/FNWI/Education Service Centre", "ESC" },
            { "UvA/FNWI/Projectenbureau FNWI", "FB" },
            { "UvA/FNWI/WZI", "WZI" },
            { "UvA/FNWI/API", "API" },
            { "UvA/FNWI/College of Sciences", "CoSS" },
            { "UvA/FNWI/Voorlichting & Communicatie FNWI", "FB" },
            { "UvA/FNWI/Bestuurszaken FNWI", "FB" },
            { "UvA/FNWI/Technologie Centrum FNWI", "FB" },
            { "UvA/FNWI/HEF", "HEF" },
            { "UvA/FNWI", "FB" },
            { "UvA/FNWI/Graduate School of Life and Earth Sciences", "GSLES" },
            { "UvA/FNWI/Marktontwikkeling FNWI", "FB" },
            { "UvA/FNWI/Planning & Control FNWI", "FB" },
            { "UvA/FNWI/Graduate School of Informatics", "GSI" },
            { "UvA/FNWI/Gebouwen, Arbo & Milieu FNWI", "FB" },
            { "UvA/FNWI/SILS", "SILS" },
            { "UvA/FNWI/College of Informatics", "CoI" },
            { "UvA/FNWI/HIMS", "HIMS" },
            { "UvA/FNWI/IVI", "IVI" },
            { "UvA/FNWI/Directie FNWI", "FB" },
            { "UvA/FNWI/ITF", "ITFA" },
        };
    }
}
