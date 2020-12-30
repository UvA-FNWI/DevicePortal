using DevicePortal.Data;
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

        [HttpPost]
        public IActionResult Import()
        {
            if (Request.Form.Files.Count != 2)
            {
                return BadRequest("Unexpected file count");
            }

            var monthlyReport = Request.Form.Files.FirstOrDefault(f => f.FileName.Contains("Maandrapportage"));
            var secureSelfReport = Request.Form.Files.FirstOrDefault(f => !f.FileName.Contains("Maandrapportage"));
            var monthlyReportParsed = CsvParser.Parse(monthlyReport);
            var secureSelfReportParsed = CsvParser.Parse(secureSelfReport);

            int iFaculty = -1, iInstitute = -1, iUserName = -1, iUserFullName = -1, iDeviceName = -1, iDeviceType = -1, iLinkedTo = -1;
            for (int i = 0; i < monthlyReportParsed.header.Count; ++i)
            {
                switch (monthlyReportParsed.header[i])
                {
                    case "Computer-nummer": iDeviceName = i; break;
                    case "Faculteit": if (iFaculty == -1) { iFaculty = i; } break; // duplicate entry in sheet
                    case "Gebruikersnaam/Kamer": iUserName = i; break;
                    case "Persoon/Gebouw": iUserFullName = i; break;
                    case "Soort": iDeviceType = i; break;
                    case "Klantorganisatie": iInstitute = i; break;
                    case "Gekoppeld aan": iLinkedTo = i; break;
                }
            }
            if (iFaculty == -1 || iInstitute == -1 || iUserName == -1 || iDeviceName == -1 || iDeviceType == -1 || iLinkedTo == -1)
            {
                return BadRequest("Invalid Maandrapportage file. Incorrect headers.");
            }

            int iSerial = -1, iObjectId = -1;
            for (int i = 0; i < secureSelfReportParsed.header.Count; ++i)
            {
                switch (secureSelfReportParsed.header[i])
                {
                    case "Object ID": iObjectId = i; break;                    
                    case "Serienummer (Algemene gegevens)": iSerial = i; break;
                }
            }
            if (iSerial == -1 || iObjectId == -1)
            {
                return BadRequest("Invalid Self Service file. Incorrect headers.");
            }

            Dictionary<string, string> serialMap = new Dictionary<string, string>();
            foreach (var line in secureSelfReportParsed.lines) 
            {
                if (!string.IsNullOrEmpty(line[iSerial])) 
                {
                    serialMap.Add(line[iObjectId], line[iSerial]);
                }
            }

            Dictionary<string, User> userMap = _context.Users.ToDictionary(u => u.UserName);
            List<User> usersToAdd = new List<User>();
            Dictionary<string, Device> deviceMap = _context.Devices.ToDictionary(d => d.Name);
            List<Device> devicesToAdd = new List<Device>();
            List<Device> devicesToUpdate = new List<Device>();
            foreach (var line in monthlyReportParsed.lines)
            {
                if (line[iLinkedTo] != "Persoon") { continue; }

                var device = new Device
                {                   
                    Name = line[iDeviceName],
                    UserName = line[iUserName],
                    SerialNumber = serialMap.TryGetValue(line[iDeviceName], out string serial) ? serial : line[iDeviceName],
                    Origin = DeviceOrigin.DataExport,
                    Status = DeviceStatus.Denied,
                };

                string deviceType = line[iDeviceType];
                if (deviceType.StartsWith("Desktop")) { device.Type = DeviceType.Desktop; }
                else if (deviceType.StartsWith("Laptop")) { device.Type = DeviceType.Laptop; }
                else if (deviceType.StartsWith("Tablet")) { device.Type = DeviceType.Tablet; }
                else if (deviceType.StartsWith("Mobiel")) { device.Type = DeviceType.Mobile; }

                if (deviceType.EndsWith("Apple")) { device.OS = "macOS"; }
                else if (deviceType.EndsWith("Windows")) { device.OS = "Windows"; }
                else if (deviceType.EndsWith("Linux")) { device.OS = "Linux"; }

                if (!string.IsNullOrEmpty(device.UserName))
                {
                    if (!userMap.ContainsKey(device.UserName))
                    {
                        var user = new User()
                        {
                            Faculty = line[iFaculty]["UvA/".Length..],
                            Institute = line[iInstitute].Length > line[iFaculty].Length ?
                                line[iInstitute][(line[iFaculty].Length + 1)..] : // + 1 for /
                                "", // Institute could equal Faculty
                            Name = line[iUserFullName],
                            UserName = device.UserName,
                            CanApprove = false,
                            CanSecure = false,
                        };
                        usersToAdd.Add(user);
                        userMap.Add(user.UserName, user);
                    }

                    if (deviceMap.TryGetValue(device.Name, out Device existing))
                    {
                        if (existing.UserName != device.UserName ||
                            existing.SerialNumber != device.SerialNumber) 
                        {
                            existing.UserName = device.UserName;
                            existing.SerialNumber = device.SerialNumber;
                            devicesToUpdate.Add(existing);
                        }
                    }
                    else { devicesToAdd.Add(device); }
                }
            }
            if (devicesToUpdate.Any()) 
            {
                _context.Devices.UpdateRange(devicesToUpdate);
                _context.SaveChanges();
            }

            // https://www.michalbialecki.com/2020/05/03/entity-framework-core-5-vs-sqlbulkcopy-2/
            var connection = _context.Database.GetDbConnection() as SqlConnection;
            connection.Open();
            using var sqlBulk = new SqlBulkCopy(_context.Database.GetDbConnection() as SqlConnection);

            // Bulk insert users
            var userTable = new System.Data.DataTable();
            userTable.Columns.Add("Faculty");
            userTable.Columns.Add("Institute");
            userTable.Columns.Add("Name");
            userTable.Columns.Add("UserName");
            userTable.Columns.Add("CanSecure");
            userTable.Columns.Add("CanApprove");
            userTable.Columns.Add("CanAdmin");
            foreach (var user in usersToAdd)
            {
                userTable.Rows.Add(user.Faculty, user.Institute, user.Name, user.UserName, user.CanSecure, user.CanApprove, user.CanAdmin);
            }
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Faculty", "Faculty"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Institute", "Institute"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Name", "Name"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("UserName", "UserName"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanSecure", "CanSecure"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanApprove", "CanApprove"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("CanAdmin", "CanAdmin"));
            sqlBulk.DestinationTableName = "dbo.Users";
            sqlBulk.WriteToServer(userTable);

            // Bulk insert devices
            var deviceTable = new System.Data.DataTable();
            deviceTable.Columns.Add("UserName");
            deviceTable.Columns.Add("Name");
            deviceTable.Columns.Add("Type", typeof(int));
            deviceTable.Columns.Add("OS");
            deviceTable.Columns.Add("Origin", typeof(int));
            deviceTable.Columns.Add("Status", typeof(int));
            foreach (var device in devicesToAdd)
            {
                deviceTable.Rows.Add(device.UserName, device.Name, device.Type, device.OS, device.Origin, device.Status);
            }

            sqlBulk.ColumnMappings.Clear();
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("UserName", "UserName"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Name", "Name"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Type", "Type"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("OS", "OS"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Origin", "Origin"));
            sqlBulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Status", "Status"));
            sqlBulk.DestinationTableName = "dbo.Devices";
            sqlBulk.WriteToServer(deviceTable);
            sqlBulk.Close();
            connection.Close();

            return Ok();
        }
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
