with RelevantDevices as (
   select * from Devices dv 
   where dv.Category in (4,8) and dv.Type in (4,8) and dv.Name not like '%Chrome%' and dv.Status < 4
),
UserSummary as (
	select u.*,
	(case when u.CanSecure = 1 then 1 else 0 end) as PortalAuthorized,
	(case when exists(select * from SecurityChecks c where c.UserName = u.Username) then 1 else 0 end) as CheckSubmitted,
	(case when exists(select * from RelevantDevices d where d.UserName = u.UserName and d.Status < 3) then 1 else 0 end) as SecuredDevice
	from Users u 
	where exists (select * from RelevantDevices d where d.UserName = u.UserName)
)
select count(*),
sum(PortalAuthorized),
sum(CheckSubmitted),
sum(SecuredDevice - CheckSubmitted),
sum(SecuredDevice)
from UserSummary