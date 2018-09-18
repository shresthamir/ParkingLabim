ALTER TABLE tblSetting ADD EnableStamp BIT
ALTER TABLE tblSetting ADD EnableStaff BIT
ALTER TABLE tblSetting ADD EnableDiscount BIT
GO
CREATE TABLE [dbo].[DiscountScheme](
	[SchemeId] [int] NOT NULL,
	[SchemeName] [varchar](100) NOT NULL,
	[ValidOnWeekends] [bit] NOT NULL,
	[ValidOnHolidays] [bit] NOT NULL,
	[ValidHours] [varchar](250) NOT NULL,
	[DiscountPercent] [numeric](5, 2) NOT NULL,
	[DiscountAmount] [varchar](250) NULL,
	[MinHrs] [int] NOT NULL,
	[MaxHrs] [int] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
 CONSTRAINT [PK_DiscountScheme] PRIMARY KEY CLUSTERED 
(
	[SchemeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO MENU VALUES (50, 'DISCOUNT SCHEME',	2, NULL, 'ParkingManagement.Forms.Master.ucDiscountScheme')
SELECT * FROM DateMiti
