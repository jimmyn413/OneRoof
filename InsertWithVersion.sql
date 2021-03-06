--table supported POSTS with versioning without any PUTS.  Line was added to check for last version and increment by 1.


ALTER PROC [dbo].[JobDesignInsert]
	        @JobId int
           ,@StructuralUpgradeRequired bit
           ,@StructuralStampRequired bit
           ,@RackingManufacturer varchar(50)
           ,@LoadJustificationRequired bit
           ,@IncentiveAmountPerkWh decimal(4,2)
           ,@MspUpgradeBussRating varchar(10)
           ,@MspUpgradeMainBreakerRating varchar(10)
           ,@MspUpgradeMainBreakerLocation varchar(50)
           ,@HomeownerFinalDesignApproval bit
           ,@UserName varchar(50)
as

BEGIN

	INSERT INTO [dbo].[JobDesign]
           ([Version]
		   ,[JobId]
           ,[StructuralUpgradeRequired]
           ,[StructuralStampRequired]
           ,[RackingManufacturer]
           ,[LoadJustificationRequired]
           ,[IncentiveAmountPerkWh]
           ,[MspUpgradeBussRating]
           ,[MspUpgradeMainBreakerRating]
           ,[MspUpgradeMainBreakerLocation]
           ,[HomeownerFinalDesignApproval]
           ,[CreatedByUserId]
           ,[ModifiedByUserId]
		   ,[CreatedDate]
		   ,[CreatedBy]
		   ,[ModifiedDate]
		   ,[ModifiedBy]
			)

     VALUES
           ((SELECT ISNULL(MAX(Version), 0)+1 FROM [dbo].[JobDesign] WHERE jobid = @JobId)
		   ,@JobId
           ,@StructuralUpgradeRequired
           ,@StructuralStampRequired
           ,@RackingManufacturer
           ,@LoadJustificationRequired
           ,@IncentiveAmountPerkWh
           ,@MspUpgradeBussRating
           ,@MspUpgradeMainBreakerRating
           ,@MspUpgradeMainBreakerLocation
           ,@HomeownerFinalDesignApproval
		   ,dbo.GetUserIdByUserName(@UserName)
		   ,dbo.GetUserIdByUserName(@UserName)
		   ,GETDATE()
		   ,dbo.udf_ReturnDBUser(50)
		   ,GETDATE()
		   ,dbo.udf_ReturnDBUser(50)
			)

		   SELECT SCOPE_IDENTITY()
END