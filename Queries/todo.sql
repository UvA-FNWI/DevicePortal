select u.Name, u.Email, u.UserName, d.Name from Users u
inner join Users_Departments ud on ud.UserName = u.UserName
  inner join Departments d on d.Id = ud.DepartmentId
where exists ( select * from SecurityChecks sc where sc.UserName = u.UserName )
and u.CanSecure = 1 
and exists (select * from Devices d where d.UserName = u.UserName and d.Status <> 0)
order by d.Name


select u.Name, u.Email, u.UserName from Users u
where exists ( select * from SecurityChecks sc where sc.UserName = u.UserName )
and u.CanSecure = 1 
--and exists (select * from Devices d where d.UserName = u.UserName and d.Status <> 0)