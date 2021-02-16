using DevicePortal.Data;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MimeKit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevicePortal
{
    public class NotificationTask : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public NotificationTask(IServiceScopeFactory scopeFactory) 
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromDays(7);            
            DateTime nextRunTime = GetNextWeekday(DateTime.Today, DayOfWeek.Monday).AddHours(9);
            var firstInterval = nextRunTime > DateTime.Now ?
                nextRunTime.Subtract(DateTime.Now) :
                nextRunTime.AddDays(7).Subtract(DateTime.Now);

            Task.Run(() => 
            {
                Task.Delay(firstInterval).Wait();
                NotifyApprovers();

                // timer repeates call to NotifyApprovers every monday at 0900.
                _timer = new Timer(
                    NotifyApprovers,
                    null,
                    TimeSpan.Zero,
                    interval
                );
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void NotifyApprovers(object _ = null)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<PortalContext>();
            var Configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string host = Configuration["SMTP:Host"];
            string userName = Configuration["SMTP:UserName"];
            string password = Configuration["SMTP:Password"];
            string port_str = Configuration["SMTP:Port"];
            string sender = Configuration["NotificationCenter:Sender"];
            string senderEmail = Configuration["NotificationCenter:SenderEmail"];
            string portalUrl = Configuration["NotificationCenter:PortalUrl"];
            string subject = Configuration["NotificationCenter:PendingChecksSubject"];
            if (string.IsNullOrEmpty(host) || !int.TryParse(port_str, out int port)) { return; }

            var departmentIds = db.SecurityChecks
                .Where(c => c.Status == DeviceStatus.Submitted)
                .Select(c => c.Device.DepartmentId)
                .ToArray();
            var departmentCounts = departmentIds
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());
            var departmentIdSet = departmentIds.ToHashSet();

            var departmentMap = db.Departments
                .Where(d => departmentIdSet.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToDictionary(d => d.Id, d => d.Name);
            var approvers = db.Users
                .Where(u => u.CanApprove && u.Departments.Any(d => departmentIdSet.Contains(d.DepartmentId)))
                .Select(u => new { u.Name, u.Email, DepartmentIds = u.Departments.Select(d => d.DepartmentId) })
                .ToArray();

            using var client = new SmtpClient();
            client.Connect(host, port, false);
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            { 
                client.Authenticate(userName, password);
            }

            foreach (var approver in approvers) 
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(sender, senderEmail));
                message.To.Add(new MailboxAddress(approver.Name, approver.Email));
                message.Subject = subject;

                HashSet<int> userDepartmentIds = approver.DepartmentIds
                    .Intersect(departmentIdSet)
                    .ToHashSet();
                string[] departmentNames = userDepartmentIds
                    .Select(id => departmentMap[id])
                    .ToArray();
                int count = userDepartmentIds
                    .Select(id => departmentCounts[id])
                    .Sum();
                message.Body = new TextPart("html")
                {
                    Text = MailBody(approver.Name, departmentNames, count, portalUrl),
                };
                client.Send(message);
            }

            client.Disconnect(true);            
        }

        private static string MailBody(string approver, string[] departmentNames, int deviceCount, string portalUrl) => @$"
    <!doctype html>
    <html>
      <head>
        <meta name=""viewport"" content=""width=device-width"" />
        <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
        <title>Simple Transactional Email</title>
        <style>
            img {{
                border: none;
                -ms-interpolation-mode: bicubic;
                max-width: 100%; 
            }}
            body {{
                background-color: #f6f6f6;
                font-family: sans-serif;
                -webkit-font-smoothing: antialiased;
                font-size: 14px;
                line-height: 1.4;
                margin: 0;
                padding: 0;
                -ms-text-size-adjust: 100%;
                -webkit-text-size-adjust: 100%; 
            }}
            table {{
                border-collapse: separate;
                mso-table-lspace: 0pt;
                mso-table-rspace: 0pt;
                width: 100%; }}
                table td {{
                    font-family: sans-serif;
                    font-size: 14px;
                    vertical-align: top; 
            }}

            .body {{
                background-color: #f6f6f6;
                width: 100%; 
            }}
        
            .container {{
                display: block;
                margin: 0 auto !important;
                /* makes it centered */
                max-width: 580px;
                padding: 10px;
                width: 580px; 
            }}        
            .content {{
                box-sizing: border-box;
                display: block;
                margin: 0 auto;
                max-width: 580px;
                padding: 10px; 
            }}

            .main {{
                background: #ffffff;
                border-radius: 3px;
                width: 100%; 
            }}
            .wrapper {{
                box-sizing: border-box;
                padding: 20px; 
            }}
            .content-block {{
                padding-bottom: 10px;
                padding-top: 10px;
            }}

            .footer {{
                clear: both;
                margin-top: 10px;
                text-align: center;
                width: 100%; 
            }}
            .footer td,
            .footer p,
            .footer span,
            .footer a {{
                color: #999999;
                font-size: 12px;
                text-align: center; 
            }}

            h1,
            h2,
            h3,
            h4 {{
                color: #000000;
                font-family: sans-serif;
                font-weight: 400;
                line-height: 1.4;
                margin: 0;
                margin-bottom: 30px; 
            }}
            h1 {{
                font-size: 35px;
                font-weight: 300;
                text-align: center;
                text-transform: capitalize; 
            }}
            p,
            ul,
            ol {{
                font-family: sans-serif;
                font-size: 14px;
                font-weight: normal;
                margin: 0;
                margin-bottom: 15px; 
            }}
            p li,
            ul li,
            ol li {{
                list-style-position: inside;
                margin-left: 5px; 
            }}
            a {{
                color: #3498db;
                text-decoration: underline; 
            }}

            .btn {{
            box-sizing: border-box;
            width: 100%; }}
            .btn > tbody > tr > td {{
                padding-bottom: 15px; }}
            .btn table {{
                width: auto; 
            }}
            .btn table td {{
                background-color: #ffffff;
                border-radius: 5px;
                text-align: center; 
            }}
            .btn a {{
                background-color: #ffffff;
                border: solid 1px #3498db;
                border-radius: 5px;
                box-sizing: border-box;
                color: #3498db;
                cursor: pointer;
                display: inline-block;
                font-size: 14px;
                font-weight: bold;
                margin: 0;
                padding: 6px 12px;
                text-decoration: none;
                text-transform: capitalize; 
            }}
            .btn-primary table td {{
                background-color: #3498db; 
            }}
            .btn-primary a {{
                background-color: #bc0031;
                border-color: #bc0031;
                color: #ffffff; 
            }}

            @media only screen and (max-width: 620px) {{
                table[class=body] h1 {{
                    font-size: 28px !important;
                    margin-bottom: 10px !important; 
                }}
                table[class=body] p,
                table[class=body] ul,
                table[class=body] ol,
                table[class=body] td,
                table[class=body] span,
                table[class=body] a {{
                    font-size: 16px !important; 
                }}
                table[class=body] .wrapper,
                table[class=body] .article {{
                    padding: 10px !important; 
                }}
                table[class=body] .content {{
                    padding: 0 !important; 
                }}
                table[class=body] .container {{
                    padding: 0 !important;
                    width: 100% !important; 
                }}
                table[class=body] .main {{
                    border-left-width: 0 !important;
                    border-radius: 0 !important;
                    border-right-width: 0 !important; 
                }}
                table[class=body] .btn table {{
                    width: 100% !important; 
                }}
                table[class=body] .btn a {{
                    width: 100% !important; 
                }}
                table[class=body] .img-responsive {{
                    height: auto !important;
                    max-width: 100% !important;
                    width: auto !important; 
                }}
            }}
        </style>
    </head>
    <body>    
        <table role = ""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""body"">
            <tr>
            <td>&nbsp;</td>
            <td class=""container"">
                <div class=""content"">

                <table role = ""presentation"" class=""main"">
                    <tr>
                    <td class=""wrapper"">
                        <table role = ""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td>
                            <p>Dear {approver},</p>
                            <p>For {string.Join(',', departmentNames)}, there are {deviceCount} devices with pending security checks. Please go to <a href=""{portalUrl}"">{portalUrl}</a> or click the button below to accept or reject the checks.</p>
                            <table role = ""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""btn btn-primary"">
                                <tbody>
                                <tr>
                                    <td align = ""left"" >
                                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                                        <tbody>
                                        <tr>
                                            <td><a href = ""{portalUrl}"" target=""_blank"">Go to Device portal</a> </td>
                                        </tr>
                                        </tbody>
                                    </table>
                                    </td>
                                </tr>
                                </tbody>
                            </table>
                            <p>Best regards,</p>
                            <p>Faculty of Science IT,</br>
                                secure-science@uva.nl</p>
                            </td>
                        </tr>
                        </table>
                    </td>
                    </tr>
                </table>
            
                <div class=""footer"">
                    <table role = ""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">               
                    <tr>
                        <td class=""content-block powered-by"">
                        University of Amsterdam, Faculty of Science IT
                        </td>
                    </tr>
                    </table>
                </div>            
                </div>
            </td>
            <td>&nbsp;</td>
            </tr>
        </table>
    </body>
</html>";

        private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }
    }

    public class RightsTask : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public RightsTask(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromHours(24);
            DateTime nextRunTime = DateTime.Today.AddDays(1);
            var firstInterval = nextRunTime.Subtract(DateTime.Now);

            Task.Run(() =>
            {
                Task.Delay(firstInterval).Wait();
                UpdateRights();

                // timer repeates call to NotifyApprovers every monday at 0900.
                _timer = new Timer(
                    UpdateRights,
                    null,
                    TimeSpan.Zero,
                    interval
                );
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void UpdateRights(object _ = null)
        {
            await Run();
        }

        public async Task Run() 
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<PortalContext>();
            var departmentService = scope.ServiceProvider.GetRequiredService<DepartmentService>();

            int facultyId = db.Faculties.Select(f => f.Id).First();
            var departments = db.Departments.ToArray();
            var departmentIdMap = departments.ToDictionary(d => d.Id);
            var departmentNameMap = new ConcurrentDictionary<string, Department>(departments.ToDictionary(d => d.Name));
            var users = db.Users
                .Include(u => u.Departments)
                .ToArray();

            int threads = 8;
            var threadData = new List<User>[threads];
            int i = 0;
            for (; i < threads; ++i) { threadData[i] = new List<User>(); }
            i = 0;
            foreach (var user in users)
            {
                threadData[i++].Add(user);
                i %= threads;
            }

            i = 0;
            var departmentsToAdd = new ConcurrentBag<Department>();
            var userDepartsToAdd = new ConcurrentBag<User_Department>();
            var userDepartsToUpdate = new ConcurrentBag<User_Department>();
            var userDepartsToRemove = new ConcurrentBag<User_Department>();
            var tasks = new Task[threads];
            for (; i < threads; ++i)
            {
                var data = threadData[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var user in data) 
                    {
                        var rights = await departmentService.GetDepartments(user.UserName);
                        var rightsMap = new Dictionary<string, DepartmentService.Department>(); // contains duplicates
                        foreach (var right in rights) { rightsMap.TryAdd(right.Name, right); }

                        // Update or remove existings departments
                        foreach (var ud in user.Departments)
                        {
                            if (departmentIdMap.TryGetValue(ud.DepartmentId, out var department) &&
                                rightsMap.TryGetValue(department.Name, out var right))
                            {
                                if (ud.CanManage != right.IsManager) 
                                {
                                    ud.CanManage = right.IsManager;
                                    userDepartsToUpdate.Add(ud);
                                }
                            }
                            else { userDepartsToRemove.Add(ud); }
                        }

                        // Add new departments to user
                        var currentDepartmentIds = user.Departments.Select(ud => ud.DepartmentId).ToHashSet();
                        foreach (var right in rightsMap.Values)
                        {
                            if (!departmentNameMap.TryGetValue(right.Name, out var department) ||
                                !currentDepartmentIds.Contains(department.Id))
                            {
                                var user_department = new User_Department()
                                {
                                    UserName = user.UserName,
                                    CanManage = right.IsManager,
                                };
                                if (department != null)
                                {
                                    user_department.DepartmentId = department.Id;
                                }
                                else
                                {
                                    user_department.Department = new Department { Name = right.Name, FacultyId = facultyId };
                                    if (departmentNameMap.TryAdd(right.Name, user_department.Department)) 
                                    {
                                        departmentsToAdd.Add(user_department.Department);
                                    }
                                }
                                userDepartsToAdd.Add(user_department);
                            }
                        }
                    }
                });
            }
            Task.WhenAll(tasks).Wait();

            db.Departments.AddRange(departmentsToAdd);
            await db.SaveChangesAsync();

            foreach (var ud in userDepartsToUpdate) { var entry = db.Entry(ud); entry.Property(p => p.CanManage).IsModified = true; }
            foreach (var ud in userDepartsToAdd) { if (ud.Department != null) { ud.DepartmentId = ud.Department.Id; } }
            db.Users_Departments.AddRange(userDepartsToAdd);
            db.Users_Departments.RemoveRange(userDepartsToRemove);
            await db.SaveChangesAsync();
        }
    }
}
