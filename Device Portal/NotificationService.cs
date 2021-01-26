using DevicePortal.Data;
using MailKit.Net.Smtp;
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
    public class NotificationService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public NotificationService(IServiceScopeFactory scopeFactory) 
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

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }
    }
}
