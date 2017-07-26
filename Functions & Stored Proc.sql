
CREATE PROCEDURE [dbo].[SP_CREATE_USER] 
@UNAME VARCHAR(100),
@PWD VARCHAR(100)
AS
BEGIN
IF NOT EXISTS (SELECT COUNT(*) FROM SYS.SYSLOGINS WHERE NAME = @UNAME)
EXEC('CREATE LOGIN ['+@UNAME+'] WITH PASSWORD = '''+@PWD+'''')
IF NOT EXISTS (SELECT * FROM SYS.sysusers WHERE name = @UNAME)
	BEGIN
		EXEC('CREATE USER ['+@UNAME+'] FOR LOGIN ['+@UNAME+']')
		EXEC('GRANT SELECT, INSERT ON SCHEMA::DBO TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::VehicleType TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::RateMaster TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::RateDetails TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::PARKINGAREA TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::Users TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::UserRight TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::tblSHIFT TO ['+@UNAME+']')
		EXEC('GRANT UPDATE, DELETE ON OBJECT::tblStaff TO ['+@UNAME+']')
		EXEC('GRANT UPDATE ON OBJECT::SESSION TO [' + @UNAME + ']')
		EXEC('GRANT UPDATE ON OBJECT::tblSequence TO [' + @UNAME + ']')
		EXEC('GRANT EXECUTE ON OBJECT::sp_Calculate_PCharge TO [' + @UNAME + ']')
		EXEC('GRANT EXECUTE ON OBJECT::Num_ToWordsArabic TO [' + @UNAME + ']')
		EXEC('GRANT EXECUTE ON OBJECT::sp_UserWLogDetail TO [' + @UNAME + ']')
		EXEC('GRANT EXECUTE ON OBJECT::GetDayOfWeek TO [' + @UNAME + ']')
		EXEC('GRANT EXECUTE ON OBJECT::IsInvoiceActive TO [' + @UNAME + ']')
	END
END

GO

CREATE FUNCTION [dbo].[Num_ToWordsArabic] (
@Number Numeric (38,0) -- Input number with as many as 18 digits
) RETURNS VARCHAR(8000)

AS BEGIN

DECLARE @inputNumber VARCHAR(38)
DECLARE @NumbersTable TABLE (number CHAR(2), word VARCHAR(10))
DECLARE @outputString VARCHAR(8000)
DECLARE @length INT
DECLARE @counter INT
DECLARE @loops INT
DECLARE @position INT
DECLARE @chunk CHAR(3) -- for chunks of 3 numbers
DECLARE @tensones CHAR(2)
DECLARE @hundreds CHAR(1)
DECLARE @tens CHAR(1)
DECLARE @ones CHAR(1)
DECLARE @BelowThousands VARCHAR(100)
IF @Number = 0 Return 'Zero'

-- initialize the variables
SELECT @inputNumber = CONVERT(varchar(38), @Number)
, @outputString = ''
, @counter = 1
SET @BelowThousands = RIGHT(@inputNumber,3)
--SET @BelowThousands = dbo.Num_ToWords(CAST(RIGHT(@inputNumber,3) as numeric))
if(LEN(@inputNumber)>3)
	SET @inputNumber = LEFT(@inputNumber,LEN(@inputNumber)-3)
ELSE 
	SET @inputNumber = ''
SELECT @length = LEN(@inputNumber)
, @position = LEN(@inputNumber) - 1
, @loops = LEN(@inputNumber)/2

-- make sure there is an extra loop added for the remaining numbers
IF LEN(@inputNumber) % 2 <> 0 SET @loops = @loops + 1

-- insert data for the numbers and words
INSERT INTO @NumbersTable SELECT '00', ''
UNION ALL SELECT '01', 'one' UNION ALL SELECT '02', 'two'
UNION ALL SELECT '03', 'three' UNION ALL SELECT '04', 'four'
UNION ALL SELECT '05', 'five' UNION ALL SELECT '06', 'six'
UNION ALL SELECT '07', 'seven' UNION ALL SELECT '08', 'eight'
UNION ALL SELECT '09', 'nine' UNION ALL SELECT '10', 'ten'
UNION ALL SELECT '11', 'eleven' UNION ALL SELECT '12', 'twelve'
UNION ALL SELECT '13', 'thirteen' UNION ALL SELECT '14', 'fourteen'
UNION ALL SELECT '15', 'fifteen' UNION ALL SELECT '16', 'sixteen'
UNION ALL SELECT '17', 'seventeen' UNION ALL SELECT '18', 'eighteen'
UNION ALL SELECT '19', 'nineteen' UNION ALL SELECT '20', 'twenty'
UNION ALL SELECT '30', 'thirty' UNION ALL SELECT '40', 'forty'
UNION ALL SELECT '50', 'fifty' UNION ALL SELECT '60', 'sixty'
UNION ALL SELECT '70', 'seventy' UNION ALL SELECT '80', 'eighty'
UNION ALL SELECT '90', 'ninety'

WHILE @counter <= @loops 
BEGIN

	SET @chunk = RIGHT('00' + SUBSTRING(@inputNumber, @position, 2), 2)

	IF @chunk <> '00' 
	BEGIN
		SELECT @tensones = SUBSTRING(@chunk, 1, 2)
				 , @tens = SUBSTRING(@chunk, 1, 1)
				 , @ones = SUBSTRING(@chunk, 2, 1)


		-- If twenty or less, use the word directly from @NumbersTable
		IF CONVERT(INT, @tensones) <= 20 OR @Ones='0' 
		BEGIN
			SET @outputString = (SELECT word FROM @NumbersTable WHERE @tensones = number)
					+ CASE @counter WHEN 1 THEN ' thousand ' -- No name
					WHEN 2 THEN ' lakh ' WHEN 3 THEN ' crore '
					WHEN 4 THEN ' arab ' WHEN 5 THEN ' kharab '
					Else '' END
					+ @outputString
		End
		ELSE 
		BEGIN -- break down the ones and the tens separately

			SET @outputString = ' ' + (SELECT word + '-' FROM @NumbersTable WHERE @tens + '0' = number)
									+ (SELECT word FROM @NumbersTable Where '0'+ @ones = number) 
									+ CASE @counter WHEN 1 THEN ' thousand ' -- No name
									WHEN 2 THEN ' lakh ' WHEN 3 THEN ' crore '
									WHEN 4 THEN ' arab ' WHEN 5 THEN ' kharab '
									Else '' END
									+ @outputString
		END
	End

	SELECT @counter = @counter + 1
		, @position = @position - 2
End


SET @chunk = RIGHT('000' + @BelowThousands, 3)
IF @chunk <> '000' 
BEGIN
	SELECT @tensones = SUBSTRING(@chunk, 2, 2)
		 , @hundreds = SUBSTRING(@chunk, 1, 1)
		     , @tens = SUBSTRING(@chunk, 2, 1)
			 , @ones = SUBSTRING(@chunk, 3, 1)

	-- If twenty or less, use the word directly from @NumbersTable
	IF CONVERT(INT, @tensones) <= 20 OR @Ones='0' 
		SET @BelowThousands = (SELECT word FROM @NumbersTable WHERE @tensones = number)
	ELSE 
		SET @BelowThousands = (SELECT word + '-' FROM @NumbersTable WHERE @tens + '0' = number)
									+ (SELECT word FROM @NumbersTable Where '0'+ @ones = number) 
	-- now get the hundreds
	IF @hundreds <> '0' 
	BEGIN
		SET @BelowThousands = (SELECT word FROM @NumbersTable Where '0' + @hundreds = number) + ' hundred ' + @BelowThousands
	End
END
-- Remove any double spaces
SET @outputString = LTRIM(RTRIM(REPLACE(@outputString, ' ', ' ')))
SET @outputstring = UPPER(LEFT(@outputstring, 1)) + SUBSTRING(@outputstring, 2, 8000)
SET @BelowThousands = LTRIM(RTRIM(@BelowThousands))

IF @BelowThousands <> '000' AND @outputString <> ''	
	SET @outputString = @outputString + ' ' + LOWER(@BelowThousands)
else if @outputString = ''	
	SET @outputString = UPPER(LEFT(@BelowThousands, 1)) + SUBSTRING(@BelowThousands, 2, 8000)
RETURN @outputString -- return the result
End





GO

CREATE PROCEDURE [dbo].[sp_UserWLogDetail] 

@Flg as int =1,
@SDate as smalldatetime,
@EDate as smalldatetime,
@UserID as varchar(100)='%',
@HostNM as varchar(200)='%',
@FormNM as varchar(250)='%',
@ActionNM as varchar(100)='%'

AS

IF @Flg=1 
BEGIN
SELECT WL.UserId, UL.HOSTNAME, FormName, TrnDate,TrnTime,TrnMode,WorkDetail,VchrNo,Remarks FROM TBLUSERWORKINGLOG WL INNER JOIN [SESSION] UL ON WL.USERSESSID=UL.SESSION_ID
	JOIN USERS U ON UL.UID = U.UID
	WHERE FORMNAME LIKE @FormNM AND U.UserName LIKE @UserId AND UL.HOSTNAME LIKE @HostNM AND TrnMode LIKE @ActionNM AND TRNDATE BETWEEN @SDate And @EDate --AND USERSESSID=USERSESSID
	ORDER BY TRNDATE,CAST(TRNTIME AS SMALLDATETIME),WL.USERID,FORMNAME
END	
	
ELSE IF @Flg=2
BEGIN
SELECT WL.UserId, FormName, 
		SUM(CASE WHEN TRNMODE='New' THEN 1 ELSE 0 END) AS TOT_NEW, 
		SUM(CASE WHEN TRNMODE='Edit' THEN 1 ELSE 0 END) AS TOT_EDIT,
		SUM(CASE WHEN TRNMODE='Delete' THEN 1 ELSE 0 END) AS TOT_DELETE, 
		SUM(CASE WHEN TRNMODE='Re-Print' THEN 1 ELSE 0 END) AS TOT_REPRINT, 
		SUM(CASE WHEN TRNMODE='View' THEN 1 ELSE 0 END) AS TOT_VIEW, 
		SUM(CASE WHEN TRNMODE='Import' OR TRNMODE='Export' THEN 1 ELSE 0 END) AS TOT_IMP_EXP,
		SUM(CASE WHEN TRNMODE='Backup' OR TRNMODE='Restore' THEN 1 ELSE 0 END) AS TOT_BKP_RES  
	FROM TBLUSERWORKINGLOG WL INNER JOIN TBLUSERLOGINLOG UL ON WL.USERSESSID=UL.USERSESSID
	WHERE FORMNAME LIKE @FormNM AND UL.USERID LIKE @UserId AND UL.HOSTNAME LIKE @HostNM  AND TrnMode LIKE @ActionNM AND TRNDATE BETWEEN @SDate And @EDate --AND USERSESSID=USERSESSID
	GROUP BY WL.UserId, FormName
	ORDER BY WL.USERID,FORMNAME	
END

ELSE IF @Flg=3
BEGIN
SELECT WL.UserId, 
		SUM(CASE WHEN TRNMODE='New' THEN 1 ELSE 0 END) AS TOT_NEW, 
		SUM(CASE WHEN TRNMODE='Edit' THEN 1 ELSE 0 END) AS TOT_EDIT,
		SUM(CASE WHEN TRNMODE='Delete' THEN 1 ELSE 0 END) AS TOT_DELETE, 
		SUM(CASE WHEN TRNMODE='Re-Print' THEN 1 ELSE 0 END) AS TOT_REPRINT, 
		SUM(CASE WHEN TRNMODE='View' THEN 1 ELSE 0 END) AS TOT_VIEW, 
		SUM(CASE WHEN TRNMODE='Import' OR TRNMODE='Export' THEN 1 ELSE 0 END) AS TOT_IMP_EXP,
		SUM(CASE WHEN TRNMODE='Backup' OR TRNMODE='Restore' THEN 1 ELSE 0 END) AS TOT_BKP_RES 
	FROM TBLUSERWORKINGLOG WL INNER JOIN TBLUSERLOGINLOG UL ON WL.USERSESSID=UL.USERSESSID
	WHERE FORMNAME LIKE @FormNM AND UL.USERID LIKE @UserId AND UL.HOSTNAME LIKE @HostNM  AND TrnMode LIKE @ActionNM AND TRNDATE BETWEEN @SDate And @EDate --AND USERSESSID=USERSESSID
	GROUP BY WL.UserId
	ORDER BY WL.USERID	
END


GO

CREATE FUNCTION [dbo].[IsInvoiceActive]
(
	@BillNo VARCHAR(20),
	@FYID INT
)
RETURNS TINYINT
AS
BEGIN
	DECLARE @ROWS INT;
	DECLARE @RES TINYINT;
	SELECT @ROWS = COUNT(*) FROM ParkingSales 
	WHERE RefBillNo = @BillNo AND FYID = @FYID

	if @ROWS > 0
		SET @RES = 0
	ELSE 
		SET @RES = 1
	RETURN @RES
	
END

GO

CREATE TRIGGER [dbo].[PARKINGSALES_DELETE] ON [dbo].[ParkingSales] INSTEAD OF DELETE AS 
BEGIN
RAISERROR('DELETE PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO

CREATE TRIGGER [dbo].[PARKINGSALES_UPDATE] ON [dbo].[ParkingSales] INSTEAD OF UPDATE AS 
BEGIN
RAISERROR('EDIT PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO


CREATE TRIGGER [dbo].[PID_DELETE] ON [dbo].[ParkingInDetails] INSTEAD OF DELETE AS 
BEGIN
RAISERROR('DELETE PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO


CREATE TRIGGER [dbo].[PID_UPDATE] ON [dbo].[ParkingInDetails] INSTEAD OF UPDATE AS 
BEGIN
RAISERROR('EDIT PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO



CREATE TRIGGER [dbo].[POD_DELETE] ON [dbo].[ParkingOutDetails] INSTEAD OF DELETE AS 
BEGIN
RAISERROR('DELETE PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO

CREATE TRIGGER [dbo].[POD_UPDATE] ON [dbo].[ParkingOutDetails] INSTEAD OF UPDATE AS 
BEGIN
RAISERROR('EDIT PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO
CREATE TRIGGER [dbo].[TRAILLOG_DELETE] ON [dbo].[tblUserWorkingLog] INSTEAD OF DELETE AS 
BEGIN
RAISERROR('DELETE PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO
CREATE TRIGGER [dbo].[TRAILLOG_UPDATE] ON [dbo].[tblUserWorkingLog] INSTEAD OF UPDATE AS 
BEGIN
RAISERROR('EDIT PREVENTED BY ADMINISTRATOR',16,1)
END
RETURN

GO

CREATE VIEW [dbo].[VIEW_ANNEX7]
AS
SELECT F.FYNAME FISCAL_YEAR, BillNo BILL_NO,BILLTO CUSTOMER_NAME,BILLTOPAN CUSTOMER_PAN,TDATE BILL_DATE,TAXABLE + NONTAXABLE + Discount  AMOUNT, DISCOUNT,TAXABLE TAXABLE_AMOUNT,VAT TAX_AMOUNT,1 IS_PRINTED, DBO.IsInvoiceActive(BILLNO,PS.FYID) IS_ACTIVE,TTIME PRINTED_TIME,
USERNAME ENTERED_BY,USERNAME PRINTED_BY,TDATE AS EDATE  FROM ParkingSales PS JOIN tblFiscalYear F ON PS.FYID = F.FYID JOIN Users U ON PS.UID = U.UID  WHERE LEFT(BillNo,2) IN ('SI','TI')

GO







