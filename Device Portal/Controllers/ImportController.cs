using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly PortalContext _context;

        public ImportController(PortalContext portalContext)
        {
            _context = portalContext;
        }

        [HttpPost, Authorize(Policy = AppPolicies.AdminOnly)]
        public IActionResult Import()
        {
            if (Request.Form.Files.Count != 1)
            {
                return BadRequest("Unexpected file count. Expected data file.");
            }

            var deviceExportFile = Request.Form.Files[0];
            var deviceExport = CsvParser.Parse(deviceExportFile);

            int iDepartment = -1, iUserName = -1, iUserEmail = -1, iBrand = -1, iBrandType = -1, iDeviceId = -1, iDeviceType = -1, 
                iDeviceOS = -1, iSerial = -1, iCostCentre = -1, iTracsBuilding = -1, iTracsRoom = -1, iTracsOutlet = -1,
                iMacadres = -1, iNotes = -1, iPurchasedDate = -1, iLastSeenDate = -1;
            for (int i = 0; i < deviceExport.header.Count; ++i)
            {
                switch (deviceExport.header[i])
                {
                    case "naam": iDeviceId = i; break;
                    case "serienummer": iSerial = i; break;
                    case "login_gebruiker": iUserName = i; break;
                    case "email": iUserEmail = i; break;
                    case "soort": iDeviceType = i; break;
                    case "klantorganisatie": iDepartment = i; break;
                    case "besturingssysteem": iDeviceOS = i; break;
                    case "merk": iBrand = i; break;
                    case "type": iBrandType = i; break;
                    case "kostenplaats": iCostCentre = i; break;
                    case "itracs_gebouw": iTracsBuilding = i; break;
                    case "itracs_ruimte": iTracsRoom = i;  break;
                    case "itracs_outlet": iTracsOutlet = i; break;
                    case "macadres": iMacadres = i; break;
                    case "notities_klant": iNotes = i; break;
                    case "datum_laatst_gezien": iLastSeenDate = i; break;
                    case "aanschafdatum": iPurchasedDate = i; break;
                }
            }
            if (iDepartment == -1 || iUserName == -1 || iDeviceId == -1 || iDeviceType == -1)
            {
                return BadRequest("Invalid Maandrapportage file. Incorrect headers.");
            }

            var faculty = _context.Faculties.FirstOrDefault();
            var departmentMap = _context.Departments.ToDictionary(d => d.Name);

            DateTime now = DateTime.Now;            
            Dictionary<string, User> userMap = _context.Users.ToDictionary(u => u.UserName);
            HashSet<string> activeDeviceSet = new HashSet<string>();
            List<User> usersToAdd = new List<User>();
            List<User> usersToUpdate = new List<User>();
            Dictionary<string, Device> deviceMap = _context.Devices
                .Where(d => d.DeviceId != null) // Note(Joshua): DeviceId is null with Origin.User
                .ToArray()
                .ToDictionary(d => d.DeviceId.ToLower());
            List<Device> devicesToAdd = new List<Device>();
            List<Device> devicesToUpdate = new List<Device>();
            var deviceHistoriesToAdd = new List<DeviceHistory>();
            List<Department> departmentsToAdd = new List<Department>();
            foreach (var line in deviceExport.lines)
            {                
                string departmentName = instituteDepartmentMap.TryGetValue(line[iDepartment], out departmentName) ?
                           departmentName : line[iDepartment];
                if (!departmentMap.TryGetValue(departmentName, out Department department))
                {
                    department = new Department { Name = departmentName, FacultyId = faculty.Id };
                    departmentMap.Add(departmentName, department);
                    departmentsToAdd.Add(department);
                }

                var device = new Device
                {
                    Name = $"{line[iBrand]} {line[iBrandType]}".Trim(),
                    DeviceId = line[iDeviceId],
                    UserName = string.IsNullOrEmpty(line[iUserName]) ? null : line[iUserName],
                    SerialNumber = line[iSerial],
                    Origin = DeviceOrigin.DataExport,
                    Status = DeviceStatus.Unsecure,
                    StatusEffectiveDate = now,
                    Department = department,
                    CostCentre = line[iCostCentre],
                    ItracsBuilding = !string.IsNullOrEmpty(line[iTracsBuilding]) && 
                                     buildingNameMap.TryGetValue(line[iTracsBuilding], out string name) ?
                                     name : line[iTracsBuilding],
                    ItracsOutlet = line[iTracsOutlet],
                    ItracsRoom = line[iTracsRoom],
                    LastSeenDate = !string.IsNullOrEmpty(line[iLastSeenDate]) ? DateTime.Parse(line[iLastSeenDate]) : default,
                    Macadres = line[iMacadres],
                    Notes = line[iNotes],
                    PurchaseDate = !string.IsNullOrEmpty(line[iPurchasedDate]) ? DateTime.Parse(line[iPurchasedDate]) : default,
                };
                activeDeviceSet.Add(device.DeviceId.ToLower());

                string deviceType = line[iDeviceType];
                if (deviceType.StartsWith("Desktop")) { device.Type = DeviceType.Desktop; }
                else if (deviceType.StartsWith("Laptop")) { device.Type = DeviceType.Laptop; }
                else if (deviceType.StartsWith("Tablet")) { device.Type = DeviceType.Tablet; }
                else if (deviceType.StartsWith("Mobiel")) { device.Type = DeviceType.Mobile; }

                if (deviceType.Contains("UvA Standaard SCCM")) { device.Category = DeviceCategory.ManagedStandard; }
                else if (deviceType.Contains("UvA Spec. SCCM")) { device.Category = DeviceCategory.ManagedSpecial; }                
                else if (deviceType.Contains("UvA Zelfsupp.")) { device.Category = DeviceCategory.SelfSupport; }
                else { device.Category = DeviceCategory.Other; }

                string deviceOs = line[iDeviceOS];
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
                    device.OS_Type = line[iBrand].Contains("Apple") ? OS_Type.iOS : OS_Type.Android;
                }

                if (!string.IsNullOrEmpty(device.UserName))
                {
                    if (userMap.TryGetValue(device.UserName, out var user))
                    {
                        if (user.Email != line[iUserEmail])
                        {
                            user.Email = line[iUserEmail];
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
                            Email = line[iUserEmail],
                        };
                        usersToAdd.Add(user);
                        userMap.Add(user.UserName, user);
                    }
                }

                if (deviceMap.TryGetValue(device.DeviceId.ToLower(), out Device existing))
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
                        existing.Name != device.Name ||
                        existing.PurchaseDate != device.PurchaseDate ||
                        existing.Notes != device.Notes ||
                        existing.Status == DeviceStatus.Disposed)
                    {
                        deviceHistoriesToAdd.Add(new DeviceHistory(existing));

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
                _context.AddRange(departmentsToAdd);
                _context.SaveChanges();
            }

            // https://www.michalbialecki.com/2020/05/03/entity-framework-core-5-vs-sqlbulkcopy-2/
            var connection = _context.Database.GetDbConnection() as SqlConnection;
            connection.Open();
            using var sqlBulk = new SqlBulkCopy(_context.Database.GetDbConnection() as SqlConnection);

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

            // Update users
            if (usersToUpdate.Any())
            {
                _context.Users.UpdateRange(usersToUpdate);
                _context.SaveChanges();
            }

            // Update devices
            if (devicesToUpdate.Any())
            {
                _context.DeviceHistories.AddRange(deviceHistoriesToAdd);
                _context.Devices.UpdateRange(devicesToUpdate);
                _context.SaveChanges();
            }
            // Dispose devices
            if (activeDeviceSet.Count < deviceMap.Count)
            {
                var disposed = deviceMap.Values
                    .Where(d => !activeDeviceSet.Contains(d.DeviceId.ToLower()) && d.Origin == DeviceOrigin.DataExport);
                foreach (var d in disposed)
                {
                    _context.DeviceHistories.Add(new DeviceHistory(d));

                    d.Status = DeviceStatus.Disposed;
                    d.StatusEffectiveDate = now;
                    _context.UpdateProperties(d, dd => dd.Status, dd => dd.StatusEffectiveDate);
                }
                _context.SaveChanges();
            }

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

            return Ok();
        }

        private readonly Dictionary<string, OS_Type> osTypeMap = new Dictionary<string, OS_Type>()
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

    public static class CsvParser
    {
        public static (List<string> header, List<string[]> lines) Parse(IFormFile file)
        {
            var encoding = Encoding.GetEncoding(Encoding.Default.CodePage, EncoderFallback.ReplacementFallback, new Latin1Fallback());
            using var stream = new StreamReader(file.OpenReadStream(), encoding);

            // settings
            bool trimStartEnd = true;
            char delimiter = ';', delimiterB = ',', quote = '"';

            var sbField = new StringBuilder();
            string content = stream.ReadToEnd();
            int iEnd = content.Length - 1;
            List<string> readHeaderLine(char delimiter, out int i)
            {
                // Note(Joshua): does not include eol in col val.
                var columns = new List<string>();
                bool whitespaceStart = true, inQuotes = false;
                for (i = 0; i < content.Length; ++i)
                {
                    if (!char.IsWhiteSpace(content[i])) { whitespaceStart = false; }

                    bool endOfLine = i == iEnd || content[i] == '\n' || (content[i] == '\r' && content[i + 1] == '\n');
                    if ((endOfLine || content[i] == delimiter) && !inQuotes)
                    {
                        // Trim end
                        whitespaceStart = true;
                        int index = sbField.Length - 1;
                        while (index >= 0)
                        {
                            if (!char.IsWhiteSpace(sbField[index])) { break; }
                            index--;
                        }
                        columns.Add(sbField.ToString(0, index >= 0 ? index + 1 : sbField.Length));
                        sbField.Clear();

                        if (endOfLine)
                        {
                            if (content[i + 1] == '\n') { ++i; }
                            break;
                        }
                    }
                    else if (content[i] == quote)
                    {
                        int index = i;
                        while (content[++index] == quote)
                        {
                            // "" => "
                            if ((index - i) % 2 == 0)
                            {
                                sbField.Append(quote);
                            }
                        }
                        // If uneven count of quotes => inquotes
                        if ((index - i) % 2 == 1) { inQuotes = !inQuotes; }
                        i = index - 1;
                    }
                    else if ((!trimStartEnd || !whitespaceStart) && !endOfLine) // No line end in header
                    {
                        sbField.Append(content[i]);
                    }
                }
                return columns;
            }

            List<string> headers = readHeaderLine(delimiter, out int i);
            List<string> headersB = readHeaderLine(delimiterB, out int iB);
            if (headers.Count < headersB.Count)
            {
                i = iB;
                headers = headersB;
                delimiter = delimiterB;
            }

            int iColumn = 0;
            bool whitespaceStart = true, inQuotes = false;
            string[] columns = new string[headers.Count];
            List<string[]> lines = new List<string[]>();
            for (++i; i < content.Length; ++i)
            {
                if (!char.IsWhiteSpace(content[i])) { whitespaceStart = false; }

                bool endOfLine = i == iEnd || content[i] == '\n' || (content[i] == '\r' && content[i + 1] == '\n');
                if ((endOfLine || content[i] == delimiter) && !inQuotes)
                {
                    // Trim end
                    whitespaceStart = true;
                    int index = sbField.Length - 1;
                    while (index >= 0)
                    {
                        if (!char.IsWhiteSpace(sbField[index])) { break; }
                        index--;
                    }
                    columns[iColumn++] = sbField.ToString(0, index >= 0 ? index + 1 : sbField.Length);
                    sbField.Clear();

                    if (endOfLine)
                    {
                        lines.Add(columns);
                        columns = new string[headers.Count];
                        iColumn = 0;
                        if (content[i + 1] == '\n') { ++i; }
                    }
                }
                else if (content[i] == quote)
                {
                    int index = i;
                    while (content[++index] == quote)
                    {
                        // "" => "
                        if ((index - i) % 2 == 0)
                        {
                            sbField.Append(quote);
                        }
                    }
                    // If uneven count of quotes => inquotes
                    if ((index - i) % 2 == 1) { inQuotes = !inQuotes; }
                    i = index - 1;
                }
                else if (!trimStartEnd || !whitespaceStart)
                {
                    sbField.Append(content[i]);
                }
            }

            return (headers, lines);
        }
    }
    public class Latin1Fallback : DecoderFallback
    {
        public string DefaultString;

        public Latin1Fallback() : this("*")
        {
        }

        public Latin1Fallback(string defaultString)
        {
            this.DefaultString = defaultString;
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new CustomMapperFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get { return 1; }
        }
    }
    public class CustomMapperFallbackBuffer : DecoderFallbackBuffer
    {
        // Fallback encoding
        readonly Encoding latin1 = Encoding.GetEncoding("iso-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        // Index of character to return
        int index = -1;
        Latin1Fallback fb;
        string charsToReturn;

        public CustomMapperFallbackBuffer(Latin1Fallback fallback)
        {
            this.fb = fallback;
        }

        public override bool Fallback(byte[] bytesUnknown, int stringIndex)
        {
            // Return false if there are already characters to map.
            if (index >= 1) return false;

            charsToReturn = "";
            try
            {
                string chars = latin1.GetString(bytesUnknown);
                charsToReturn += chars;
                index = charsToReturn.Length - 1;
                return true;
            }
            catch { return false; }
        }

        public override char GetNextChar()
        {
            // If count is less than zero, we've returned all characters.
            return index >= 0 ? charsToReturn[index--] : '\u0000';
        }

        public override bool MovePrevious()
        {
            if (index < charsToReturn.Length - 1)
            {
                index++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int Remaining
        {
            get { return index < 0 ? 0 : index + 1; }
        }

        public override void Reset()
        {
            index = -1;
        }
    }
}
