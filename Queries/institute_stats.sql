/****** Script for SelectTopNRows command from SSMS  ******/
SELECT d.Name as Department, 
count(*) as Users,
sum(case when u.CanSecure = 1 then 1 else 0 end) as PortalAuthorized,
sum(case when sc.DeviceId is not null then 1 else 0 end) as ChecksSubmitted
  FROM [DevicePortal].[dbo].[Users] u 
  inner join Users_Departments ud on ud.UserName = u.UserName
  inner join Departments d on d.Id = ud.DepartmentId
  left join SecurityChecks sc on sc.UserName = u.UserName
  where d.Name not in ('Extern', 'FALW', 'FEW', 'FGw', 'UvA', 'FNWI', 'AUC')
  group by d.Name
  having sum(case when u.CanSecure = 1 then 1 else 0 end)  > 0