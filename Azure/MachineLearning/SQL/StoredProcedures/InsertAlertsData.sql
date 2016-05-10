CREATE PROCEDURE [dbo].[InsertAlertsData]
	@dataList [dbo].[AlertsDataTableType] READONLY
AS
	insert into [dbo].[AlertsData] ([value], [guid], [displayname], [organization], [unitofmeasure], [measurename], [location], [timecreated])
		select [value], [guid], [displayname], [organization], [unitofmeasure], [measurename], [location], [timecreated] from @dataList
RETURN 0
