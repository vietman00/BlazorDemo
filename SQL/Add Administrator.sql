-- Add Administrator Role if not already present..
if (not exists(select top 1 * from aspnetroles where name = 'Administrator'))
begin
	insert into aspnetroles (id, name, normalizedname, concurrencystamp)
	values(1, 'Administrator', 'Administrator', newid());
end

-- Get userId and assign Administrator Role..
declare @userId uniqueidentifier
declare @adminId int

select @userId = Id from aspnetusers where email = '' -- Email addressed used when registering on the site..
select @adminId = Id from aspnetroles where normalizedname = 'Administrator'

-- Show Ids to make sure we have something..
select @userId, @adminId

if (@userId IS NOT NULL and @adminId IS NOT NULL)
begin
	insert into aspnetuserroles(UserId, RoleId)
	values(@userId, @adminId)
end