﻿using DevicePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevicePortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize(Policy = AppPolicies.ManagerOnly)]
    public class FacultiesController : ControllerBase
    {
        private readonly PortalContext _context;

        public FacultiesController(PortalContext context)
        {
            _context = context;
        }

        // GET: api/Faculties
        [HttpGet]
        public async Task<ActionResult> GetOverview()
        {
            var userName = User.GetUserName();

            HashSet<int> departmentIds;
            if (User.HasClaim(AppClaimTypes.Permission, AppClaims.CanAdmin) ||
                User.HasClaim(AppClaimTypes.Permission, AppClaims.CanManageFaculty))
            {
                departmentIds = _context.Departments
                    .Select(d => d.Id)
                    .ToHashSet();
            }
            else
            {
                departmentIds = _context.Users_Departments
                    .Where(u => u.UserName == userName && u.CanManage)
                    .Select(u => u.DepartmentId)
                    .ToHashSet();
            }

            var departments = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.ParentDepartmentId,
                    Devices = d.Devices,
                }).ToArrayAsync();
            var departmentMap = departments.ToDictionary(d => d.Id);
            var childMap = departments.Where(d => d.ParentDepartmentId != null).ToLookup(d => d.ParentDepartmentId);
            var parentMap = departments.Where(d => d.ParentDepartmentId != null)
                .ToLookup(d => d.Id, d => departmentMap[(int)d.ParentDepartmentId]);
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserName,
                    u.CanApprove,
                    u.CanSecure,
                    Departments = u.Departments.Select(d => d.DepartmentId).ToHashSet(),
                })
                .ToArrayAsync();
            var usersMap = users.ToDictionary(u => u.UserName);
            var userNameSet = new HashSet<string>();

            var departmentUsers = new Dictionary<int, HashSet<string>>();
            {
                var parentAdded = new HashSet<int>();

                foreach (var user in users)
                {
                    parentAdded.Clear();

                    foreach (int departmentId in user.Departments)
                    {
                        if (!departmentUsers.TryGetValue(departmentId, out var userIds))
                        {
                            userIds = new HashSet<string>();
                            departmentUsers.Add(departmentId, userIds);
                        }
                        userIds.Add(user.UserName);

                        var parents = parentMap[departmentId];
                        foreach (var parent in parents)
                        {
                            if (!parentAdded.Add(parent.Id)) { continue; }

                            if (!departmentUsers.TryGetValue(parent.Id, out var parentUserIds))
                            {
                                parentUserIds = new HashSet<string>();
                                departmentUsers.Add(parent.Id, parentUserIds);
                            }
                            parentUserIds.Add(user.UserName);
                        }
                    }
                }
            }

            foreach (var department in departments)
            {
                var children = childMap[department.Id];
                foreach (var child in children)
                {
                    foreach (var device in child.Devices) { department.Devices.Add(device); }
                }
            }

            var departmentStats = new List<Department>();
            foreach (var department in departments)
            {
                int devicesIntuneCompleted = 0, devicesCheckSubmitted = 0, devicesCheckApproved = 0;
                foreach (var d in department.Devices)
                {
                    if (d.Status == DeviceStatus.Approved)
                    {
                        if (d.Origin == DeviceOrigin.Intune) { ++devicesIntuneCompleted; }
                        else { ++devicesCheckApproved; }
                    }
                    else if (d.Origin != DeviceOrigin.Intune && d.Status == DeviceStatus.Submitted)
                    {
                        ++devicesCheckSubmitted;
                    }
                }

                var deviceGroups = department.Devices
                    .Where(d => ! string.IsNullOrEmpty(d.UserName))
                    .GroupBy(d => d.UserName);
                int usersIntuneCompleted = 0, usersCheckSubmitted = 0, usersCheckApproved = 0, usersManagedDevices = 0;
                foreach (var group in deviceGroups)
                {
                    userNameSet.Add(group.Key);

                    if (!departmentUsers.TryGetValue(department.Id, out var depUsers) ||
                        !depUsers.Contains(group.Key))
                    {
                        continue;
                    }

                    if (group.All(d => d.Category == DeviceCategory.ManagedSpecial || d.Category == DeviceCategory.ManagedStandard || d.Category == DeviceCategory.PackageControl))
                    {
                        ++usersManagedDevices;
                    }
                    else
                    {
                        bool submitted = false;
                        bool intuneCompleted = false;
                        foreach (var d in group.AsEnumerable())
                        {
                            // Intune
                            if (d.Origin == DeviceOrigin.Intune)
                            {
                                if (d.Status == DeviceStatus.Approved) { intuneCompleted = true; }
                            }
                            // Device portal checks
                            else
                            {
                                if (d.Status == DeviceStatus.Approved) { ++usersCheckApproved; }
                                else
                                {
                                    submitted = submitted || d.Status == DeviceStatus.Submitted;
                                }
                            }
                        }
                        if (submitted) { ++usersCheckSubmitted; }
                        if (intuneCompleted) { ++usersIntuneCompleted; }
                    }
                }

                departmentStats.Add(new Department
                {
                    Id = department.Id,
                    ParentDepartmentId = department.ParentDepartmentId ?? 0,
                    Name = department.Name,
                    Devices = department.Devices.Count,
                    DevicesBYOD = department.Devices.Count(d => d.Category == DeviceCategory.BYOD),
                    DevicesManaged = department.Devices.Count(d => d.Category == DeviceCategory.ManagedStandard || d.Category == DeviceCategory.ManagedSpecial || d.Category == DeviceCategory.PackageControl),
                    DevicesSelfSupport = department.Devices.Count(d => d.Category == DeviceCategory.SelfSupport),
                    DevicesCheckApproved = devicesCheckApproved,
                    DevicesCheckSubmitted = devicesCheckSubmitted,
                    DevicesIntuneCompleted = devicesIntuneCompleted,
                    Users = departmentUsers.TryGetValue(department.Id, out var u) ? u.Count : 0,
                    UsersAuthorized = departmentUsers.TryGetValue(department.Id, out u) ? u.Count(u => usersMap[u].CanSecure) : 0,
                    UsersApprover = departmentUsers.TryGetValue(department.Id, out u) ? u.Count(u => usersMap[u].CanApprove) : 0,
                    UsersCheckApproved = usersCheckApproved,
                    UsersCheckSubmitted = usersCheckSubmitted,
                    UsersIntuneCompleted = usersIntuneCompleted,
                    UsersManagedDevices = usersManagedDevices,
                });
            }

            // Filter users based on device presence in department, User_Department is incomplete
            users = users.Where(u => u.Departments.Overlaps(departmentIds) || userNameSet.Contains(u.UserName)).ToArray();
            return Ok(new
            {
                Departments = departmentStats,
                Users = users.Length,
                UsersAuthorized = users.Count(u => u.CanSecure),
                UsersApprover = users.Count(u => u.CanApprove),
            });
        }

        class Department
        {
            public int Id { get; set; }
            public int ParentDepartmentId { get; set; }
            public string Name { get; set; }
            public int Users { get; set; }
            public int UsersAuthorized { get; set; }
            public int UsersApprover { get; set; }
            public int UsersIntuneCompleted { get; set; }
            public int UsersManagedDevices { get; set; }
            public int UsersCheckSubmitted { get; set; }
            public int UsersCheckApproved { get; set; }
            public int Devices { get; set; }
            public int DevicesManaged { get; set; }
            public int DevicesSelfSupport { get; set; }
            public int DevicesBYOD { get; set; }
            public int DevicesIntuneCompleted { get; set; }
            public int DevicesCheckSubmitted { get; set; }
            public int DevicesCheckApproved { get; set; }
        }
    }
}
