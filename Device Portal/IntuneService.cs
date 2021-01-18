using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal
{
    public class IntuneService
    {
        public static ClientCredentialProvider authProvider;
        private readonly PortalContext _context;

        public IntuneService(PortalContext context)
        {
            _context = context;
        }

        public async Task<IDeviceManagementManagedDevicesCollectionPage> GetManagedDevices()
        {
            var graphClient = GetGraphClient();
            return await graphClient.DeviceManagement.ManagedDevices.Request().GetAsync();            
        }

        public async Task<IUserManagedDevicesCollectionPage> GetManagedDevicesUser(string userId)
        {
            var graphClient = GetGraphClient();
            return await graphClient.Users[userId].ManagedDevices.Request().GetAsync();
        }

        public async Task SyncManagedDevicecUser(string userName, string objectId)
        {
            IUserManagedDevicesCollectionPage intuneDevices;
            try
            {
                var graphClient = GetGraphClient();
                intuneDevices = await graphClient.Users[objectId].ManagedDevices.Request().GetAsync();
            }
            catch { return; }

            var deviceMap = _context.Devices
                .Where(d => d.UserName == userName && !string.IsNullOrEmpty(d.SerialNumber))
                .ToDictionary(d => d.SerialNumber);

            DateTime now = DateTime.Now;
            foreach (var intuneDevice in intuneDevices)
            {
                if (!deviceMap.TryGetValue(intuneDevice.SerialNumber, out var device))
                {
                    device = new Data.Device
                    {
                        DeviceId = intuneDevice.DeviceName,
                        Name = $"{intuneDevice.Manufacturer} {intuneDevice.Model}".Trim(),
                        SerialNumber = intuneDevice.SerialNumber,
                        Origin = DeviceOrigin.Intune,
                        OS_Type = intuneDevice.OperatingSystem switch
                        {
                            "Windows" => OS_Type.Windows,
                            "iOS" => OS_Type.iOS,
                            "Android" => OS_Type.Android,
                            "macOS" => OS_Type.MacOS,
                            "OS X" => OS_Type.MacOS,
                            _ => 0,
                        },
                        OS_Version = intuneDevice.OsVersion,
                        UserName = userName,
                        Type = intuneDevice.OperatingSystem switch
                        {
                            //"Windows" => DeviceType.Desktop | DeviceType.Laptop,
                            //"iOS" => DeviceType.Tablet | DeviceType.Mobile,
                            //"Android" => DeviceType.Tablet | DeviceType.Mobile,
                            "macOS" => DeviceType.Laptop,
                            "OS X" => DeviceType.Laptop,
                            _ => 0,
                        },
                    };
                    _context.Add(device);
                }

                if (intuneDevice.DeviceRegistrationState == DeviceRegistrationState.Registered ||
                    intuneDevice.DeviceRegistrationState == DeviceRegistrationState.ApprovalPending ||
                    intuneDevice.DeviceRegistrationState == DeviceRegistrationState.NotRegisteredPendingEnrollment)
                {
                    device.Origin = DeviceOrigin.Intune;
                    device.Status = intuneDevice.ComplianceState switch
                    {
                        ComplianceState.Compliant => DeviceStatus.Approved,
                        ComplianceState.ConfigManager => DeviceStatus.Approved,
                        ComplianceState.Conflict => DeviceStatus.Denied,
                        ComplianceState.Error => DeviceStatus.Unsecure,
                        ComplianceState.InGracePeriod => DeviceStatus.Unsecure,
                        ComplianceState.Noncompliant => DeviceStatus.Submitted,
                        ComplianceState.Unknown => DeviceStatus.Unsecure,
                        _ => DeviceStatus.Unsecure,
                    };
                    device.StatusEffectiveDate = now;

                    if (device.Id > 0)
                    {
                        _context.UpdateProperties(device, d => d.Origin, d => d.Status, d => d.StatusEffectiveDate);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task SyncManagedDevices()
        {
            var graphClient = GetGraphClient();
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.ObjectId))
                .Select(u => new
                {
                    u.ObjectId,
                    u.UserName,
                    Devices = u.Devices.Where(d => !string.IsNullOrEmpty(d.SerialNumber))
                })
                .ToArrayAsync();

            DateTime now = DateTime.Now;
            foreach (var user in users)
            {
                IUserManagedDevicesCollectionPage intuneDevices;
                try
                {
                    intuneDevices = await graphClient.Users[user.ObjectId].ManagedDevices.Request().GetAsync();
                }
                catch { continue; }

                var deviceMap = user.Devices.ToDictionary(d => d.SerialNumber);
                foreach (var intuneDevice in intuneDevices)
                {
                    if (!deviceMap.TryGetValue(intuneDevice.SerialNumber, out var device))
                    {
                        device = new Data.Device
                        {
                            DeviceId = intuneDevice.DeviceName,
                            Name = $"{intuneDevice.Manufacturer} {intuneDevice.Model}".Trim(),
                            SerialNumber = intuneDevice.SerialNumber,
                            Origin = DeviceOrigin.Intune,
                            OS_Type = intuneDevice.OperatingSystem switch
                            {
                                "Windows" => OS_Type.Windows,
                                "iOS" => OS_Type.iOS,
                                "Android" => OS_Type.Android,
                                "macOS" => OS_Type.MacOS,
                                "OS X" => OS_Type.MacOS,
                                _ => 0,
                            },
                            OS_Version = intuneDevice.OsVersion,
                            UserName = user.UserName,
                            Type = intuneDevice.OperatingSystem switch
                            {
                                //"Windows" => DeviceType.Desktop | DeviceType.Laptop,
                                //"iOS" => DeviceType.Tablet | DeviceType.Mobile,
                                //"Android" => DeviceType.Tablet | DeviceType.Mobile,
                                "macOS" => DeviceType.Laptop,
                                "OS X" => DeviceType.Laptop,
                                _ => 0,
                            },
                        };
                        _context.Add(device);
                    }

                    if (intuneDevice.DeviceRegistrationState == DeviceRegistrationState.Registered ||
                        intuneDevice.DeviceRegistrationState == DeviceRegistrationState.ApprovalPending ||
                        intuneDevice.DeviceRegistrationState == DeviceRegistrationState.NotRegisteredPendingEnrollment)
                    {
                        device.Origin = DeviceOrigin.Intune;
                        device.Status = intuneDevice.ComplianceState switch
                        {
                            ComplianceState.Compliant => DeviceStatus.Approved,
                            ComplianceState.ConfigManager => DeviceStatus.Approved,
                            ComplianceState.Conflict => DeviceStatus.Denied,
                            ComplianceState.Error => DeviceStatus.Unsecure,
                            ComplianceState.InGracePeriod => DeviceStatus.Unsecure,
                            ComplianceState.Noncompliant => DeviceStatus.Submitted,
                            ComplianceState.Unknown => DeviceStatus.Unsecure,
                        };
                        device.StatusEffectiveDate = now;

                        if (device.Id > 0)
                        {
                            _context.UpdateProperties(device, d => d.Origin, d => d.Status, d => d.StatusEffectiveDate);
                        }
                    }
                }

            }
            await _context.SaveChangesAsync();
        }

        public async Task SyncUsers()
        {
            var graphClient = GetGraphClient();
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.Email)) { continue; }

                var info = await graphClient.Users.Request()
                    .Filter($"userPrincipalName eq '{user.Email}'")
                    .Select("displayName, id")
                    .GetAsync();

                if (info.Any())
                {
                    user.ObjectId = info[0].Id;
                    user.Name = info[0].DisplayName;

                    var entry = _context.Entry(user);
                    entry.Property(u => u.ObjectId).IsModified = true;
                    entry.Property(u => u.Name).IsModified = true;
                }
            }
            await _context.SaveChangesAsync();
        }

        private static GraphServiceClient GetGraphClient()
        {
            return new GraphServiceClient(authProvider);
        }
    }
}
