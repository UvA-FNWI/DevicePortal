using DevicePortal.Data;
using MailKit.Net.Smtp;
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
            TimeSpan interval = TimeSpan.FromHours(24);            
            DateTime nextRunTime = DateTime.Today.AddDays(1);            
            var firstInterval = nextRunTime.Subtract(DateTime.Now);

            Task.Run(() => 
            {
                Task.Delay(firstInterval).Wait();
                NotifyApprovers();

                // timer repeates call to NotifyApprovers every 24 hours.
                _timer = new Timer(
                    NotifyApprovers,
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromHours(24)
                );
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void NotifyApprovers(object _ = null)
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<PortalContext>();

            var departmentIds = db.SecurityChecks
                .Where(c => c.Status == DeviceStatus.Submitted)
                .Select(c => c.Device.DepartmentId)
                .ToHashSet();
            var approvers = db.Users
                .Where(u => u.CanApprove && u.Departments.Any(d => departmentIds.Contains(d.DepartmentId)))
                .Select(u => new { u.Name, u.Email })
                .ToArray();

            using var client = new SmtpClient();
            client.Connect("localhost", 9001, false);
            //client.Authenticate("joey", "password");

            foreach (var approver in approvers) 
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Device Portal", "secure-science@uva.nl"));
                message.To.Add(new MailboxAddress(approver.Name, approver.Email));
                message.Subject = "Security checks waiting for you'?";

                message.Body = new TextPart("plain")
                {
                    Text = @$"Hi {approver.Name},

I just wanted to let you know that there are security checks waiting for you to judge?

-- Device Portal"
                };
                client.Send(message);
            }

            client.Disconnect(true);            
        }
    }
}
