DROP TABLE OpeningTransaction
GO
DROP TABLE Party
GO


CREATE TABLE [dbo].[tblUserWorkingLog](
	[LogID] [bigint] NOT NULL,
	[UserId] [varchar](30) NOT NULL,
	[UserSessId] [bigint] NOT NULL,
	[FormName] [varchar](250) NOT NULL,
	[TrnDate] [smalldatetime] NOT NULL,
	[TrnTime] [varchar](20) NOT NULL,
	[TrnMode] [varchar](50) NOT NULL,
	[WorkDetail] [nvarchar](500) NULL,
	[VchrNo] [varchar](50) NULL,
	[Remarks] [varchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[tblPrintLog](
	[BillNo] [varchar](50) NOT NULL,
	[PrintDate] [smalldatetime] NOT NULL,
	[PrintTime] [varchar](8) NOT NULL,
	[PrintUser] [varchar](50) NOT NULL,
	[PrintRemarks] [varchar](255) NULL,
	[PrintDesc] [varchar](255) NULL,
	[FYID] [smallint] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE ParkingOutDetails ADD BILLTO VARCHAR(50)
GO

ALTER TABLE ParkingOutDetails ADD BILLTOADD VARCHAR(255)
GO

ALTER TABLE ParkingOutDetails ADD BILLTOPAN VARCHAR(15)
GO

ALTER TABLE ParkingSales ADD NonTaxable NUMERIC(18,12) NOT NULL, 
CONSTRAINT DF_NonTaxable DEFAULT (0) FOR NonTaxable
GO


ALTER TABLE ParkingSales ADD BILLTOADD VARCHAR(255)
GO

ALTER TABLE ParkingSales ADD BILLTOPAN VARCHAR(15)
GO

ALTER TABLE ParkingSales ADD TaxInvoice BIT
GO

ALTER TABLE ParkingSales ADD Amount Numeric(18,12)
GO

ALTER TABLE ParkingSales ADD RefBillNo VARCHAR(15)
GO

ALTER TABLE ParkingSales DROP COLUMN CustID
GO

ALTER TABLE ParkingSales DROP CONSTRAINT PK_ParkingSales
ALTER TABLE ParkingSales ALTER COLUMN BillNo VARCHAR(15) NOT NULL
UPDATE ParkingSales SET BillNo = 'SI' + BillNo
ALTER TABLE ParkingSales ADD CONSTRAINT PK_ParkingSales Primary KEY (BillNo)

ALTER TABLE SESSION ADD HostName nvarchar(350)

ALTER TABLE USERS ADD IsInactive BIT
UPDATE USERS SET IsInactive = 0
ALTER TABLE USERS ALTER COLUMN IsInactive BIT NOT NULL











