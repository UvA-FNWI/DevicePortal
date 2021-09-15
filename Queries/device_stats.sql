SELECT d.Name as Department, 
count(*) as Devices,
sum(case when dv.Status in (0,2) and sc.Status is null then 1 else 0 end) as SecureViaIntune,
sum(case when dv.Status in (0,2) and sc.Status is not null then 1 else 0 end) as SecureViaPortal,
sum(case when u.CanSecure = 1 then 1 else 0 end) as PortalAuthorized
--sum(case when sc.DeviceId is not null then 1 else 0 end) as ChecksSubmitted
  FROM [DevicePortal].[dbo].[Users] u 
  inner join Users_Departments ud on ud.UserName = u.UserName
  inner join Devices dv on dv.UserName = u.UserName
  inner join Departments d on d.Id = dv.DepartmentId
  left join SecurityChecks sc on sc.DeviceId = dv.Id
  where d.Name not in ('Extern', 'FALW', 'FEW', 'FGw', 'UvA', 'FNWI', 'AUC') and dv.Status <= 3 and dv.Category in (4,8) and dv.Type in (4,8) and dv.Name not like '%Chrome%'
  group by d.Name
  having sum(case when u.CanSecure = 1 then 1 else 0 end)  > 0

  SELECT d.Name as Department, 
count(*) as Devices,
sum(case when dv.Status in (0,2) and sc.Status is null then 1 else 0 end) as SecureViaIntune,
sum(case when dv.Status in (0,2) and sc.Status is not null then 1 else 0 end) as SecureViaPortal
--sum(case when sc.DeviceId is not null then 1 else 0 end) as ChecksSubmitted
  FROM [DevicePortal].[dbo].Devices dv
  inner join Departments d on d.Id = dv.DepartmentId
  left join SecurityChecks sc on sc.DeviceId = dv.Id
  where d.Name not in ('Extern', 'FALW', 'FEW', 'FGw', 'UvA', 'FNWI', 'AUC') and dv.Status <= 3 and dv.Category in (4,8) and dv.Type in (4,8) and dv.Name not like '%Chrome%'
  group by d.Name
  having sum(case when dv.Status = 0 then 1 else 0 end)  > 0