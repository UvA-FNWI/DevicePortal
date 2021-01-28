using DevicePortal.Data;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MimeKit;
using System;
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
            string subject = Configuration["PendingChecksSubject"];
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
                message.Body = new TextPart("plain")
                {
                    Text = @$"Dear {approver.Name},

For {string.Join(',', departmentNames)}, there are {count} devices with pending security checks. Pelase go to {portalUrl} to accept or reject the checks.

Best regards,

Faculty of Science IT
secure-science@uva.nl",                    

                };
                client.Send(message);
            }

            client.Disconnect(true);            
        }

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

        public async void UpdateRights(object _ = null)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<PortalContext>();
            var departmentService = scope.ServiceProvider.GetRequiredService<DepartmentService>();

            int facultyId = db.Faculties.Select(f => f.Id).First();
            var departments = db.Departments.ToArray();
            var departmentIdMap = db.Departments.ToDictionary(d => d.Id);
            var departmentNameMap = db.Departments.ToDictionary(d => d.Name);
            var users = db.Users
                .Include(u => u.Departments)
                .ToArray();
            foreach (var user in users) 
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
                        ud.CanManage = right.IsManager;
                    }
                    else { db.Users_Departments.Remove(ud); }
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
                            departmentNameMap.Add(right.Name, user_department.Department);                            
                        }
                        db.Users_Departments.Add(user_department);
                    }
                }
            }
        }
    }
}
