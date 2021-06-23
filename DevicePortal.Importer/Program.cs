using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using DevicePortal.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DevicePortal.Importer
{
    public class LabnetJson
    {
        public LabnetDevice[] results;
    }

    public class LabnetDevice
    {
        public string mac;
        public string master_mac;
        public bool is_master_device;
        public string hardware_type;
        public string hardware_vendor;
        public string os_name;
        public string os_version;
        public string hostname;
        public string ipv4_address;
        public string ipv6_address;
        public string detected_interface;
        public string detected_interface_fortiletelemetry;
        public string is_detected_interface_role_wan;
        public int last_seen;
        public bool is_online;
    }

    class Program
    {
        static readonly StreamWriter logFile = new("log.txt", true);

        static readonly string HelpText = @"Usage: DevicePortal.Importer.exe [--cmdb] [--labnet [<filename>]]";

        static void Main(string[] args)
        {
            const int ERROR_BAD_ARGUMENTS = 160;

            void HelpExit()
            {
                Console.WriteLine(HelpText);
                Environment.Exit(ERROR_BAD_ARGUMENTS);
            }

            try 
            {
                var cmd_params = new List<(string cmd, List<string> parameters)>();
                foreach (string arg in args)
                {
                    if (arg.StartsWith("--"))
                    {
                        cmd_params.Add((arg, new List<string>()));
                    }
                    else
                    {
                        if (cmd_params.Count == 0) { HelpExit(); }
                        cmd_params.Last().parameters.Add(arg);
                    }
                }

                if (cmd_params.Count == 0) { HelpExit(); }

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<Program>()
                    .Build();

                foreach (var cp in cmd_params)
                {
                    switch (cp.cmd)
                    {
                        case "--cmdb":
                            ImportCmdb(config); 
                            break;

                        case "--labnet":
                            string labnetJson = null;
                            if (cp.parameters.Count == 0)
                            {
                                var sec = config.GetSection("Labnet");
                                var container = new CookieContainer();
                                var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true, CookieContainer = container });

                                client.PostAsync($"{sec["Url"]}/logincheck", new StringContent($"ajax=1&username={sec["Username"]}&secretkey={sec["Password"]}")).Wait();
                                labnetJson = client.GetStringAsync($"{sec["Url"]}/api/v2/monitor/user/detected-device?expand_child_macs=true&with_fortilink=true&with_fortiap=false&with_endpoint=true&with_dhcp=true&with_user=true").Result;
                            }
                            else if (cp.parameters.Count != 1) { HelpExit(); }
                            else
                                labnetJson = File.ReadAllText(cp.parameters[0]);
                            var labnetData = JsonConvert.DeserializeObject<LabnetJson>(labnetJson);
                            ImportLabnet(labnetData.results);
                            break;
                    }
                }
            }
            catch (Exception ex) 
            {
                Log(ex);
            }

            Log("Done");

            logFile.Flush();
            logFile.Close();
        }

        static void ImportCmdb(IConfiguration config) 
        {
            string connectionString = config.GetConnectionString("DefaultConnection");
            var options = new DbContextOptionsBuilder<PortalContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            using var portalContext = new PortalContext(options);
            using var dwhContext = new DWHPMContext();

            Log("Preparing");
            DateTime now = DateTime.Now;
            Faculty faculty = portalContext.Faculties.FirstOrDefault();
            HashSet<string> activeDeviceSet = new HashSet<string>();
            Dictionary<string, Department> departmentMap = portalContext.Departments.ToDictionary(d => d.Name);
            Dictionary<string, User> userMap = portalContext.Users.ToDictionary(u => u.UserName);
            List<User> usersToAdd = new List<User>();
            List<User> usersToUpdate = new List<User>();
            Dictionary<string, Device> deviceMap = portalContext.Devices
                .Where(d => d.DeviceId != null) // Note(Joshua): DeviceId is null with Origin.User
                .ToArray()
                .ToDictionary(d => d.DeviceId.ToLower());
            // Note: if a user has made any edit, we no longer update the device through this import 
            HashSet<int> ignoreSet = portalContext.DeviceHistories
                .Where(h => h.UserEditId != User.ImporterId && h.UserEditId != User.IntuneServiceId)
                .Select(h => h.OriginalDeviceId)
                .ToArray().ToHashSet();
            ignoreSet.UnionWith(portalContext.Devices
                .Where(d => d.UserEditId != User.ImporterId && d.UserEditId != User.IntuneServiceId)
                .Select(d => d.Id)
                .ToArray());
            List<Device> devicesToAdd = new List<Device>();
            List<Device> devicesToUpdate = new List<Device>();
            var deviceHistoriesToAdd = new List<DeviceHistory>();
            List<Department> departmentsToAdd = new List<Department>();

            Log("Getting devices from cmdb");
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
                    UserName = string.IsNullOrEmpty(d.LoginGebruiker) ? null : d.LoginGebruiker,
                    SerialNumber = d.Serienummer ?? "",
                    Origin = DeviceOrigin.DataExport,
                    Status = DeviceStatus.Unsecure,
                    StatusEffectiveDate = now,
                    Department = department,
                    DepartmentId = department.Id,
                    CostCentre = d.Kostenplaats ?? "",
                    ItracsBuilding = !string.IsNullOrEmpty(d.ItracsGebouw) && 
                                     buildingNameMap.TryGetValue(d.ItracsGebouw, out string name) ? name : (d.ItracsGebouw ?? ""),
                    ItracsOutlet = d.ItracsOutlet ?? "",
                    ItracsRoom = d.ItracsRuimte ?? "",
                    LastSeenDate = d.DatumLaatstGezien == DateTime.MinValue ? null : d.DatumLaatstGezien,
                    Macadres = d.Macadres ?? "",
                    Notes = d.NotitiesKlant ?? "",
                    PurchaseDate = d.Aanschafdatum == DateTime.MinValue ? null : d.Aanschafdatum,
                    OS_Version = "",
                };
                activeDeviceSet.Add(device.DeviceId.ToLower());

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
                }

                if (deviceMap.TryGetValue(device.DeviceId.ToLower(), out Device existing))
                {
                    if (!ignoreSet.Contains(existing.Id) &&
                        (existing.UserName != device.UserName ||
                        existing.SerialNumber != device.SerialNumber ||
                        existing.Category != device.Category ||
                        existing.CostCentre != device.CostCentre ||
                        existing.DepartmentId != device.Department.Id ||
                        existing.ItracsBuilding != device.ItracsBuilding ||
                        existing.ItracsOutlet != device.ItracsOutlet ||
                        existing.ItracsRoom != device.ItracsRoom ||
                        existing.LastSeenDate != device.LastSeenDate ||
                        existing.Macadres != device.Macadres ||
                        existing.Name != device.Name ||
                        existing.PurchaseDate != device.PurchaseDate ||
                        existing.Notes != device.Notes ||
                        existing.Status == DeviceStatus.Disposed))
                    {
                        deviceHistoriesToAdd.Add(new DeviceHistory(existing));

                        existing.UserName = device.UserName;
                        existing.DateEdit = now;
                        existing.UserEditId = User.ImporterId;
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
                        if (existing.Status == DeviceStatus.Disposed)
                        {
                            existing.Status = device.Status;
                            existing.StatusEffectiveDate = now;
                        }
                        if (device.Department != null)
                        {
                            existing.Department = device.Department;
                            existing.DepartmentId = device.Department.Id;
                        }
                        devicesToUpdate.Add(existing);
                    }
                }
                else { devicesToAdd.Add(device); }
            }

            // Insert departments
            if (departmentsToAdd.Any())
            {
                Log($"Inserting {departmentsToAdd.Count} new departments");
                portalContext.AddRange(departmentsToAdd);
                portalContext.SaveChanges();
            }

            // https://www.michalbialecki.com/2020/05/03/entity-framework-core-5-vs-sqlbulkcopy-2/
            var connection = portalContext.Database.GetDbConnection() as SqlConnection;
            connection.Open();
            using var sqlBulk = new SqlBulkCopy(portalContext.Database.GetDbConnection() as SqlConnection);

            // Bulk insert users
            Log($"Inserting {usersToAdd.Count} new users");
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

            // Update users
            if (usersToUpdate.Any())
            {
                Log($"Updating {usersToUpdate.Count} users");
                portalContext.Users.UpdateRange(usersToUpdate);
                portalContext.SaveChanges();
            }

            // Update devices
            if (devicesToUpdate.Any())
            {
                Log($"Updating {devicesToUpdate.Count} devices");
                portalContext.DeviceHistories.AddRange(deviceHistoriesToAdd);
                portalContext.Devices.UpdateRange(devicesToUpdate);
                portalContext.SaveChanges();
            }
            // Dispose devices
            if (activeDeviceSet.Count < deviceMap.Count)
            {
                var disposed = deviceMap.Values
                    .Where(d => !activeDeviceSet.Contains(d.DeviceId.ToLower()) && d.Origin == DeviceOrigin.DataExport);
                Log($"Disposing {disposed.Count()} devices");
                foreach (var d in disposed)
                {
                    portalContext.DeviceHistories.Add(new DeviceHistory(d));

                    d.DateEdit = now;
                    d.UserEditId = User.ImporterId;
                    d.Status = DeviceStatus.Disposed;
                    d.StatusEffectiveDate = now;
                    portalContext.UpdateProperties(d, dd => dd.DateEdit, dd => dd.UserEditId, 
                        dd => dd.Status, dd => dd.StatusEffectiveDate);
                }
                portalContext.SaveChanges();
            }

            // Bulk insert devices
            Log($"Inserting {devicesToAdd.Count} new devices");
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

        static void ImportLabnet(LabnetDevice[] labnetDevices)
        {
            Log("Importing Labnet devices");

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

            using var db = new PortalContext(options);

            var portalDevices = db.Devices.Active().ToArray();
            var mapPortalDevices = portalDevices.Where(d => !string.IsNullOrEmpty(d.Macadres) && d.Macadres.Length > 10).ToLookup(d => d.Macadres);
            var labnets = db.Labnets.ToArray();
            var mapLabnets = labnets.ToDictionary(l => l.Id);

            // Note: if a user or intune import has made any edit, we no longer update
            // device type, OS type and version through this import 
            HashSet<int> ignoreSet = db.DeviceHistories
                .Where(h => h.UserEditId != User.ImporterId)
                .Select(h => h.OriginalDeviceId)
                .ToArray().ToHashSet();
            ignoreSet.UnionWith(db.Devices
                .Where(d => d.UserEditId != User.ImporterId)
                .Select(d => d.Id)
                .ToArray());

            var now = DateTime.Now;

            var historiesToAdd = new List<DeviceHistory>();
            var labnetsToAdd = new List<Labnet>();
            var devicesToAdd = new List<Device>();
            var devicesToUpdate = new List<Device>();
            foreach (var ld in labnetDevices)
            {
                if (string.IsNullOrEmpty(ld.mac)) { continue; }
                if (string.IsNullOrEmpty(ld.ipv4_address)) { continue; }
                if (ld.ipv4_address.Count(c => c == '.') != 3)
                {
                    Log($"Skipping malformed ipv4 address ({ld.ipv4_address})");
                    continue;
                }

                var parts = ld.ipv4_address.Split('.', StringSplitOptions.TrimEntries);
                if (parts.Length != 4) { continue; }

                bool ipMalformed = false;
                foreach (var p in parts)
                {
                    if (!byte.TryParse(p, out byte sub))
                    {
                        Log($"Skipping malformed ipv4 address ({ld.ipv4_address})");
                        ipMalformed = true;
                        break;
                    }
                }
                if (ipMalformed) { continue; }

                int labnetId = byte.Parse(parts[2]);
                int category = byte.Parse(parts[3]);

                if (!mapLabnets.TryGetValue(labnetId, out var labnet))
                {
                    labnet = new Labnet()
                    {
                        Id = labnetId,
                        Name = $"Labnet-{labnetId}",
                    };
                    labnetsToAdd.Add(labnet);
                    mapLabnets.Add(labnetId, labnet);
                }

                var lastSeen = now.AddSeconds(-ld.last_seen);

                DeviceType deviceType = 0;
                {
                    string type = ld.hardware_type ?? "";
                    if (type.StartsWith("Computer") || type.StartsWith("Desktop")) { deviceType = DeviceType.Desktop; }
                    else if (type.StartsWith("Laptop")) { deviceType = DeviceType.Laptop; }
                    else if (type.StartsWith("Tablet")) { deviceType = DeviceType.Tablet; }
                    else if (type.StartsWith("Phone")) { deviceType = DeviceType.Mobile; }
                }

                OS_Type osType = 0;
                string osVersion = "";
                {
                    string name = ld.os_name ?? "";
                    foreach (string prefix in osTypeMap.Keys)
                    {
                        if (name.StartsWith(prefix))
                        {
                            osType = osTypeMap[prefix];
                            osVersion = name[prefix.Length..];
                        }
                    }
                    if (osType == 0 && deviceType == DeviceType.Tablet)
                    {
                        osType = ld.hardware_vendor.Contains("Apple") ? OS_Type.iOS : OS_Type.Android;
                    }
                }

                var existing = mapPortalDevices[ld.mac].FirstOrDefault();
                if (existing != null)
                {
                    bool ignore = ignoreSet.Contains(existing.Id);

                    if (existing.Ipv4 != ld.ipv4_address ||
                        existing.Ipv6 != ld.ipv6_address ||
                        existing.LabnetId != labnetId ||
                        !ignore && existing.Type != deviceType ||
                        !ignore && existing.OS_Type != osType ||
                        !ignore && existing.OS_Version != osVersion ||
                        existing.LastSeenDate == null || 
                        (lastSeen > existing.LastSeenDate && (lastSeen - existing.LastSeenDate) > TimeSpan.FromDays(1)))
                    {
                        historiesToAdd.Add(new DeviceHistory(existing));

                        existing.DateEdit = now;
                        existing.UserEditId = User.LabnetId;
                        existing.LabnetId = labnetId;
                        existing.Ipv4 = ld.ipv4_address;
                        existing.Ipv6 = ld.ipv6_address;
                        existing.LastSeenDate = lastSeen;

                        if (!ignore)
                        {
                            existing.Type = deviceType;
                            existing.OS_Type = osType;
                            existing.OS_Version = osVersion;
                        }

                        devicesToUpdate.Add(existing);
                    }
                }
                else
                {
                    if (labnet.DepartmentId != null)
                    {
                        devicesToAdd.Add(new Device()
                        {
                            Name = (!string.IsNullOrEmpty(ld.hardware_vendor) ? ld.hardware_vendor : ld.hostname) ?? "",
                            DeviceId = ld.hostname ?? "",
                            SerialNumber = "",
                            Type = deviceType,
                            Origin = DeviceOrigin.Labnet,
                            Status = DeviceStatus.Unsecure,
                            StatusEffectiveDate = now,
                            DepartmentId = (int)labnet.DepartmentId,
                            CostCentre = "",
                            ItracsBuilding = "",
                            ItracsOutlet = "",
                            ItracsRoom = "",
                            LastSeenDate = lastSeen,
                            Macadres = ld.mac,
                            Notes = "",
                            PurchaseDate = null,
                            OS_Type = osType,
                            OS_Version = osVersion,
                            Category = DeviceCategory.Other,
                            LabnetId = labnetId,
                            Ipv4 = ld.ipv4_address,
                            Ipv6 = ld.ipv6_address,
                            DateEdit = now,
                            UserEditId = User.LabnetId,
                        });
                    }
                    else
                    {
                        Log($"Not adding new device with MAC {ld.mac}, labnet {labnetId} is not linked to a department");
                    }
                }
            }

            if (labnetsToAdd.Any())
            {
                Log($"Adding {labnetsToAdd.Count} unlinked labnets");
                db.Labnets.AddRange(labnetsToAdd);
                db.SaveChanges();
            }
            
            if (devicesToAdd.Any())
            {
                Log($"Adding {devicesToAdd.Count} devices");
                db.Devices.AddRange(devicesToAdd);
                db.SaveChanges();
            }

            if (devicesToUpdate.Any())
            {
                Log($"Updating {devicesToUpdate.Count} devices");
                db.DeviceHistories.AddRange(historiesToAdd);
                db.Devices.UpdateRange(devicesToUpdate);
                db.SaveChanges();
            }
        }

        #if false
        static void Cleanup()
        {
            string connectionString = "";
            var options = new DbContextOptionsBuilder<PortalContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            using var db = new PortalContext(options);
            var devices = db.Devices.ToArray();
            var deviceMap = devices.ToDictionary(d => d.Id);
            var histories = db.DeviceHistories.OrderByDescending(h => h.DateHistory).ToArray();
            var historyMap = histories.GroupBy(h => h.OriginalDeviceId).ToDictionary(g => g.Key, g => g.ToArray());

            {
                var devicesToUpdate = new List<Device>();
                var historyToUpdate = new List<DeviceHistory>();
                foreach (var d in devices)
                {
                    if (!historyMap.TryGetValue(d.Id, out var history))
                    {
                        if (d.DateEdit == DateTime.MinValue)
                        {
                            d.DateEdit = d.StatusEffectiveDate;
                            devicesToUpdate.Add(d);
                        }
                        continue;
                    }

                    DeviceBase c = d;
                    for (int i = 0; i < history.Length; ++i)
                    {
                        var n = history[i];

                        if (c.DateEdit == DateTime.MinValue)
                        {
                            c.DateEdit = n.DateHistory;
                            if (c is Device) { devicesToUpdate.Add((Device)c); }
                            else { historyToUpdate.Add((DeviceHistory)c); }
                        }

                        c = n;
                    }
                    
                    if (c.DateEdit == DateTime.MinValue)
                    {
                        c.DateEdit = c.StatusEffectiveDate;
                        if (c is Device) { devicesToUpdate.Add((Device)c); }
                        else { historyToUpdate.Add((DeviceHistory)c); }
                    }
                }

                Console.WriteLine($"Updating {devicesToUpdate.Count} devices");
                //db.Devices.UpdateRange(devicesToUpdate);
                //db.SaveChanges();
                Console.WriteLine($"Updating {historyToUpdate.Count} histories");
                //db.DeviceHistories.UpdateRange(historyToUpdate);
                //db.SaveChanges();
            }

            {
                int countNoHistNoEdit = 0;
                var devicesToUpdate = new List<Device>();
                var historyToUpdate = new List<DeviceHistory>();
                foreach (var d in devices)
                {
                    if (!historyMap.TryGetValue(d.Id, out var history) && d.UserEditId == null)
                    {
                        ++countNoHistNoEdit;
                        Console.WriteLine($"No user edit {d.Id}");
                        devicesToUpdate.Add(d);
                        d.UserEditId = User.ImporterId;
                    }

                    if (history != null)
                    {
                        var h = history.Last();
                        if (h.UserEditId == null)
                        {
                            h.UserEditId = User.ImporterId;
                            historyToUpdate.Add(h);
                        }
                    }
                }
                Console.WriteLine($"No history and no user edit count {countNoHistNoEdit}");
                Console.WriteLine($"Updating {devicesToUpdate.Count} devices");
                Console.WriteLine($"Updating {historyToUpdate.Count} histories");
                //db.Devices.UpdateRange(devicesToUpdate);
                //db.SaveChanges();
                //db.DeviceHistories.UpdateRange(historyToUpdate);
                //db.SaveChanges();
            }

            {
                int countMatches = 0;
                var devicesToUpdate = new List<Device>();
                var historyToUpdate = new List<DeviceHistory>();
                var importDates = new List<DateTime>()
                {
                    DateTime.Parse("2021-03-23 22:20:37.4772695"),
                    DateTime.Parse("2021-03-31 11:57:45.2377984"),
                    DateTime.Parse("2021-04-21 15:17:55.2349700"),
                    DateTime.Parse("2021-04-29 11:25:54.8424557"),
                    DateTime.Parse("2021-04-30 09:25:42.9900850"),
                    DateTime.Parse("2021-05-03 20:13:59.7947545"),
                    DateTime.Parse("2021-05-04 10:05:03.8309030"),
                };

                foreach (var d in devices)
                {
                    if (!historyMap.TryGetValue(d.Id, out var history))
                    {
                        continue;
                    }

                    if (d.UserEditId == null)
                    {
                        var editDate = history[0].DateHistory;
                        foreach (var date in importDates)
                        {
                            double delta = (editDate - date).TotalSeconds;
                            if (Math.Abs(delta) < 160)
                            {
                                ++countMatches;
                                Console.WriteLine($"{d.Id} matches import date {editDate}, time diff {delta}");

                                d.UserEditId = User.ImporterId;
                                devicesToUpdate.Add(d);
                            }
                        }
                    }

                    for (int i = 0; i < history.Length; ++i)
                    {
                        var h = history[i];
                        if (h.UserEditId != null) { continue; }

                        var editDate = h == history.Last() ? h.StatusEffectiveDate : history[i + 1].DateHistory;
                        foreach (var date in importDates)
                        {
                            double delta = (editDate - date).TotalSeconds;
                            if (Math.Abs(delta) < 160)
                            {
                                ++countMatches;
                                Console.WriteLine($"{d.Id} matches import date {editDate}, time diff {delta}");

                                h.UserEditId = User.ImporterId;
                                historyToUpdate.Add(h);
                            }
                        }
                    }
                }
                Console.WriteLine($"Found {countMatches} matching import dates");
                
                Console.WriteLine($"Updating {devicesToUpdate.Count} devices");
                //db.Devices.UpdateRange(devicesToUpdate);
                //db.SaveChanges();

                Console.WriteLine($"Updating {historyToUpdate.Count} histores");
                //db.DeviceHistories.UpdateRange(historyToUpdate);
                //db.SaveChanges();
            }

            int countDupes = 0;
            var historyToRemove = new List<DeviceHistory>();
            foreach (var d in devices)
            {
                if (!historyMap.TryGetValue(d.Id, out var history))
                {
                    continue;
                }

                DeviceBase c = d;
                for (int i = 0; i < history.Length; ++i)
                {
                    var n = history[i];

                    if (c.Name              == n.Name &&
                        c.DeviceId          == n.DeviceId &&
                        c.SerialNumber      == n.SerialNumber &&
                        c.OS_Type           == n.OS_Type &&
                        c.OS_Version        == n.OS_Version &&
                        c.PurchaseDate      == n.PurchaseDate &&
                        c.CostCentre        == n.CostCentre &&
                        c.LastSeenDate      == n.LastSeenDate &&
                        c.ItracsBuilding    == n.ItracsBuilding &&
                        c.ItracsRoom        == n.ItracsRoom &&
                        c.ItracsOutlet      == n.ItracsOutlet &&
                        c.Macadres          == n.Macadres &&
                        c.Notes             == n.Notes &&
                        c.Type              == n.Type &&
                        c.Category          == n.Category &&
                        c.Status            == n.Status &&
                        c.StatusEffectiveDate == n.StatusEffectiveDate &&
                        c.Origin            == n.Origin &&
                        c.UserName          == n.UserName &&
                        c.DepartmentId      == n.DepartmentId &&
                        c.Disowned          == n.Disowned &&
                        c.Shared            == n.Shared &&
                        c.UserEditId        == n.UserEditId)
                    {
                        ++countDupes;
                        bool current = c is Device;
                        Console.WriteLine($"Duplicate found, id={n.OriginalDeviceId}, {(current ? "current" : c.Id)} - {n.Id}");

                        //if (c.PurchaseDate != n.PurchaseDate)
                        //{ 
                        //    Console.WriteLine($"PURCHASE DATE to {c.PurchaseDate} from {n.PurchaseDate}");
                        //}

                        if (!current)
                        {
                            historyToRemove.Add((DeviceHistory)c);
                        }
                        else
                        {
                            historyToRemove.Add(n);
                        }
                    }

                    c = n;
                }
            }
            Console.WriteLine($"Found {countDupes} duplicates");
            Console.WriteLine($"Removing {historyToRemove.Count} history entries");

            //db.DeviceHistories.RemoveRange(historyToRemove);
            //db.SaveChanges();
        }
        #endif

        static void Log(string msg) 
        {
            msg = $"{DateTime.Now:yyyy-MM-dd hh:mm:ff} - {msg}";
            logFile.WriteLine(msg);
            Console.WriteLine(msg);
        }
        static void Log(Exception ex) 
        {
            Log($"EXCEPTION - {ex.Message} - {ex.InnerException?.Message}");
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
            { "UvA/FNWI/KdVI", "KDV" },
            { "UvA/FNWI/Staf overig FNWI", "FB" },
            { "UvA/FNWI/ILLC", "ILLC" },
            { "UvA/FNWI/IoP", "IoP" },
            { "UvA/FNWI/IOP", "IoP" },
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
        static readonly Dictionary<string, string> buildingNameMap = new Dictionary<string, string>()
        {
            { "231 Turfdraagsterpad 1-9 (BG1)", "BG1" },
            { "355 Nieuwe Prinsengracht 130 (RE-G)", "REC-G" },
            { "630 Science Park 508 (G, AH2)", "SP 508 (G)" },
            { "640 Science Park 700 (E, AH1)", "SP 700 (E)" },
            { "642 Science Park 507 (Kassen)", "SP 507 (Kassen)" },
            { "645  Science Park 904", "SP 904" },
            { "645 	Science Park 904", "SP 904" },
            { "645 Science Park 904 (ABCD)", "SP 904 (ABCD)" },
            { "645 Sciencepark 904", "SP 904" },
            { "650 Science Park 500 (F, AH3)", "SP 500 (F)" },
            { "661 Science Park 608 B (IvI) (Startup Village)", "SP 608B (Startup Village)" },
            { "699 Science Park 107 (Nikhef, Gebouw F)", "NIKHEF F" },
            { "O2 gebouw VU", "O2 VU" },
            { "Science Park 107", "NIKHEF" },
            { "Science Park 904", "SP 904" },
            { "Science Park 904 (ABCD)", "SP 904 (ABCD)" },
            { "Science Park A", "SP 904 (A)" },
            { "SciencePark 904", "SP 904" },
            { "SP904", "SP 904" },
        };
    }
}
