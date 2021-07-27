using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevicePortal
{
    public class IntuneService
    {
        public static ClientCredentialProvider authProvider;
        private readonly PortalContext _context;

        static SemaphoreSlim semaphoreDeviceSync = new SemaphoreSlim(1, 1);
        static SemaphoreSlim semaphoreUserSync = new SemaphoreSlim(1, 1);
        static ConcurrentDictionary<string, SemaphoreSlim> semaphoreUserMap = new ConcurrentDictionary<string, SemaphoreSlim>();

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

        public async Task SyncManagedDeviceUser(string userName, string objectId)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            if (!semaphoreUserMap.TryAdd(userName, semaphore) &&
                semaphoreUserMap.TryGetValue(userName, out semaphore))
            {
                await semaphore.WaitAsync();
                return;
            }
            await semaphore.WaitAsync();

            try
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
                    .ToLookup(d => d.SerialNumber.ToLower());
                var departmentIds = _context.Users_Departments
                    .Where(ud => ud.UserName == userName)
                    .Select(ud => ud.DepartmentId)
                    .ToArray();
                if (!departmentIds.Any()) { return; }

                DateTime now = DateTime.Now;
                HashSet<string> serialSet = new HashSet<string>();
                foreach (var intuneDevice in intuneDevices.OrderByDescending(d => d.EnrolledDateTime))
                {
                    string serial_lower = intuneDevice.SerialNumber.ToLower();
                    // Skip duplicates, process entry with latest enrolled dateTime
                    if (!serialSet.Add(serial_lower)) { continue; }

                    var devices = deviceMap[serial_lower];
                    if (devices.Count() > 1) { continue; }

                    var device = devices.FirstOrDefault();
                    if (device == null)
                    {
                        device = new Data.Device
                        {
                            DepartmentId = departmentIds[0],
                            DeviceId = intuneDevice.DeviceName,
                            Name = $"{intuneDevice.Manufacturer} {intuneDevice.Model}".Trim(),
                            SerialNumber = intuneDevice.SerialNumber ?? "",
                            Origin = DeviceOrigin.Intune,
                            Category = Data.DeviceCategory.BYOD,
                            OS_Type = intuneDevice.OperatingSystem switch
                            {
                                "Windows" => OS_Type.Windows,
                                "iOS" => OS_Type.iOS,
                                "Android" => OS_Type.Android,
                                "macOS" => OS_Type.MacOS,
                                "OS X" => OS_Type.MacOS,
                                _ => 0,
                            },
                            OS_Version = intuneDevice.OsVersion ?? "",
                            UserName = userName,
                            Type = intuneDevice.OperatingSystem switch
                            {
                                "Windows" => DeviceType.Laptop,
                                "iOS" => DeviceType.Mobile,
                                "Android" => DeviceType.Mobile,
                                "macOS" => DeviceType.Laptop,
                                "OS X" => DeviceType.Laptop,
                                _ => 0,
                            },
                            Notes = "",
                            Macadres = "",
                            CostCentre = "",
                            ItracsRoom = "",
                            ItracsOutlet = "",
                            ItracsBuilding = "",
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
            finally
            {
                if (semaphoreUserMap.TryRemove(userName, out semaphore))
                {
                    semaphore.Release();
                }
            }
        }

        class UserData
        {
            public string ObjectId { get; set; }
            public string UserName { get; set; }
            public IEnumerable<Data.Device> Devices { get; set; }
            public int? DepartmentId { get; set; }
        }
        public async Task SyncManagedDevices()
        {
            await semaphoreDeviceSync.WaitAsync();

            try
            {
                var users = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.ObjectId))
                    .Select(u => new UserData
                    {
                        ObjectId = u.ObjectId,
                        UserName = u.UserName,
                        Devices = u.Devices.Where(d => !string.IsNullOrEmpty(d.SerialNumber)),
                        DepartmentId = u.Departments.Select(d => d.DepartmentId).FirstOrDefault(),
                    })
                    .ToArrayAsync();

                DateTime now = DateTime.Now;
                int threads = 8;
                var threadData = new List<UserData>[threads];
                int i = 0;
                for (; i < threads; ++i) { threadData[i] = new List<UserData>(); }
                i = 0;
                foreach (var user in users)
                {
                    threadData[i++].Add(user);
                    i %= threads;
                }

                i = 0;
                var devicesToAdd = new ConcurrentBag<Data.Device>();
                var devicesToUpdate = new ConcurrentBag<Data.Device>();
                var deviceHistoriesToAdd = new ConcurrentBag<DeviceHistory>();
                var tasks = new Task[threads];
                for (; i < threads; ++i)
                {
                    var data = threadData[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        var graphClient = GetGraphClient();
                        foreach (var user in data)
                        {
                            if (!user.DepartmentId.HasValue) { continue; }

                            IUserManagedDevicesCollectionPage intuneDevices;
                            try
                            {
                                intuneDevices = await graphClient.Users[user.ObjectId].ManagedDevices.Request().GetAsync();
                            }
                            catch { continue; }

                            var deviceMap = user.Devices.ToLookup(d => d.SerialNumber.ToLower());
                            var serialSet = new HashSet<string>();
                            foreach (var intuneDevice in intuneDevices.OrderByDescending(d => d.EnrolledDateTime))
                            {
                                string serial_lower = intuneDevice.SerialNumber.ToLower();

                                // Skip duplicates, process entry with latest enrolled dateTime
                                if (!serialSet.Add(serial_lower)) { continue; }

                                var devices = deviceMap[serial_lower];
                                if (devices.Count() > 1) { continue; }

                                var device = devices.FirstOrDefault();
                                if (device == null)
                                {
                                    device = new Data.Device
                                    {
                                        DeviceId = intuneDevice.DeviceName,
                                        Name = $"{intuneDevice.Manufacturer} {intuneDevice.Model}".Trim(),
                                        SerialNumber = intuneDevice.SerialNumber ?? "",
                                        Origin = DeviceOrigin.Intune,
                                        Category = Data.DeviceCategory.Other,
                                        OS_Type = intuneDevice.OperatingSystem switch
                                        {
                                            "Windows" => OS_Type.Windows,
                                            "iOS" => OS_Type.iOS,
                                            "Android" => OS_Type.Android,
                                            "macOS" => OS_Type.MacOS,
                                            "OS X" => OS_Type.MacOS,
                                            _ => 0,
                                        },
                                        OS_Version = intuneDevice.OsVersion ?? "",
                                        UserName = user.UserName,
                                        Type = intuneDevice.OperatingSystem switch
                                        {
                                            "Windows" => DeviceType.Laptop,
                                            "iOS" => DeviceType.Mobile,
                                            "Android" => DeviceType.Mobile,
                                            "macOS" => DeviceType.Laptop,
                                            "OS X" => DeviceType.Laptop,
                                            _ => 0,
                                        },
                                        DepartmentId = user.DepartmentId.Value,
                                        Status = DeviceStatus.Unsecure,
                                        StatusEffectiveDate = now,
                                        Notes = "",
                                        Macadres = "",
                                        CostCentre = "",
                                        ItracsRoom = "",
                                        ItracsOutlet = "",
                                        ItracsBuilding = "",
                                    };
                                    devicesToAdd.Add(device);
                                }

                                if (intuneDevice.DeviceRegistrationState == DeviceRegistrationState.Registered ||
                                    intuneDevice.DeviceRegistrationState == DeviceRegistrationState.ApprovalPending ||
                                    intuneDevice.DeviceRegistrationState == DeviceRegistrationState.NotRegisteredPendingEnrollment)
                                {
                                    if (device.Id > 0) { deviceHistoriesToAdd.Add(new DeviceHistory(device)); }

                                    device.DateEdit = now;
                                    device.UserEditId = Data.User.IntuneServiceId;
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

                                    if (device.Id > 0) { devicesToUpdate.Add(device); }
                                }
                            }

                        }
                    });
                }
                Task.WhenAll(tasks).Wait();

                if (deviceHistoriesToAdd.Any()) { _context.DeviceHistories.AddRange(deviceHistoriesToAdd); }
                foreach (var device in devicesToUpdate)
                {
                    _context.UpdateProperties(device, d => d.Origin, d => d.Status, d => d.StatusEffectiveDate);
                }
                _context.Devices.AddRange(devicesToAdd);

                await _context.SaveChangesAsync();
            }
            finally { semaphoreDeviceSync.Release(); }
        }

        public async Task<string> GetUserObjectId(string userEmail)
        {
            var graphClient = GetGraphClient();
            var info = await graphClient.Users.Request()
                .Filter($"userPrincipalName eq '{userEmail}'")
                .Select("displayName, id")
                .GetAsync();
            return info.Any() ? info[0].Id : "";
        }

        public async Task SyncUsers()
        {
            await semaphoreUserSync.WaitAsync();

            try
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
            finally { semaphoreUserSync.Release(); }
        }

        private static GraphServiceClient GetGraphClient()
        {
            return new GraphServiceClient(authProvider);
        }
    }
}
