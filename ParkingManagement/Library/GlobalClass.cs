using Dapper;
using Newtonsoft.Json.Linq;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
namespace ParkingManagement.Library
{
    struct PSlipTerms
    {
        public string Description { get; set; }
        public byte Height { get; set; }
    }
    static class GlobalClass
    {
        public static string DataConnectionString = "SERVER=IMS-D1\\SQL2017; DATABASE=PARKINGDB;UID=SA;PWD=tebahal";
        public static string Terminal = "001";
        public static int GraceTime;
        public static string CompanyName;
        public static string CompanyAddress;
        public static string CompanyPan;
        public static decimal VAT = 13;
        public static User User;
        public static string PrinterName = "POS80";
        public static PrintQueue printer;
        public static double RateTimeLinePeriodWidth = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Right - 150) / 13;
        public static Thickness FirstPeriodMargin = new Thickness(RateTimeLinePeriodWidth / 2, 0, 0, 0);
        public static DateTime BeginTime { get { return new DateTime(1900, 1, 1, 0, 0, 0, 0); } }
        public static DateTime EndTime { get { return new DateTime(1900, 1, 1, 23, 59, 59); } }
        public static IEnumerable<PSlipTerms> TCList;
        public static RateMaster DefaultRate;
        public static int Session;
        public static Visibility ShowCollectionAmountInCashSettlement;
        public static Visibility DisableCashAmountChange;
        public static Visibility DiscountVisible;
        public static Visibility StampVisible;
        public static Visibility StaffVisible;
        public static Visibility PrepaidVisible;
        public static JObject PrepaidInfo;
        public static bool EnablePlateNo { get; set; }
        public static byte AllowMultiVehicleForStaff;
        public static short DefaultMinVacantLot;
        public static string MemberBarcodePrefix;
        public static byte FYID = 1;
        public static string FYNAME;
        public static byte SettlementMode;
        public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\IMS\\Parking";
        public static decimal AbbTaxInvoiceLimit = 5000;
        internal static bool NoRawPrinter;

        public static string ReportName { get; set; }
        public static string ReportParams { get; set; }
        public static string PrintTime { get; set; }
        public static byte SlipPrinterWith { get; set; }
        static GlobalClass()
        {
            try
            {
                if (!Directory.Exists(GlobalClass.AppDataPath))
                {
                    Directory.CreateDirectory(GlobalClass.AppDataPath);
                }

                if (File.Exists(GlobalClass.AppDataPath + @"\sysPrinter.dat"))
                {
                    PrinterName = File.ReadAllText(GlobalClass.AppDataPath + @"\sysPrinter.dat");
                    printer = new PrintServer().GetPrintQueues().FirstOrDefault(x => x.FullName.Contains(PrinterName));
                }

                if (File.Exists(Environment.SystemDirectory + "\\ParkingDBSetting.dat"))
                {
                    dynamic connProps = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(Environment.SystemDirectory + "\\ParkingDBSetting.dat"));
                    DataConnectionString = string.Format("SERVER = {0}; DATABASE = {1}; UID = {2}; PWD = {3};", Encrypt(connProps.DataSource.ToString()), Encrypt(connProps.InitialCatalog.ToString()), Encrypt(connProps.UserID.ToString()), Encrypt(connProps.Password.ToString()));
                    Terminal = Encrypt(connProps.Terminal.ToString());
                }
                using (SqlConnection cnmain = new SqlConnection(DataConnectionString))
                {
                    UpdateDatabase(cnmain);
                    var Setting = cnmain.Query(@"SELECT CompanyName, CompanyAddress, CompanyInfo, ISNULL(GraceTime, 5) GraceTime, 
                                ISNULL(ShowCollectionAmountInCashSettlement, 0) ShowCollectionAmountInCashSettlement, ISNULL(DisableCashAmountChange,0) DisableCashAmountChange, 
                                SettlementMode, ISNULL(AllowMultiVehicleForStaff,0) AllowMultiVehicleForStaff, ISNULL(SlipPrinterWidth, 58) SlipPrinterWidth, 
                                ISNULL(EnableStaff, 0) EnableStaff, ISNULL(EnableStamp, 0) EnableStamp, ISNULL(EnableDiscount, 0) EnableDiscount, ISNULL(EnablePlateNo, 0) EnablePlateNo, 
                                ISNULL(EnablePrepaid, 0) EnablePrepaid, ISNULL(PrepaidInfo, '{}') PrepaidInfo, MemberBarcodePrefix FROM tblSetting").First();
                    CompanyName = Setting.CompanyName;
                    CompanyAddress = Setting.CompanyAddress;
                    CompanyPan = Setting.CompanyInfo;
                    GraceTime = Setting.GraceTime;
                    SettlementMode = Setting.SettlementMode;
                    SlipPrinterWith = Setting.SlipPrinterWidth;
                    ShowCollectionAmountInCashSettlement = ((bool)Setting.ShowCollectionAmountInCashSettlement) ? Visibility.Visible : Visibility.Collapsed;
                    DisableCashAmountChange = ((bool)Setting.DisableCashAmountChange) ? Visibility.Collapsed : Visibility.Visible;
                    DiscountVisible = ((bool)Setting.EnableDiscount) ? Visibility.Visible : Visibility.Collapsed;
                    StaffVisible = ((bool)Setting.EnableStaff) ? Visibility.Visible : Visibility.Collapsed;
                    StampVisible = ((bool)Setting.EnableStamp) ? Visibility.Visible : Visibility.Collapsed;
                    PrepaidVisible = ((bool)Setting.EnablePrepaid) ? Visibility.Visible : Visibility.Collapsed;
                    EnablePlateNo = (bool)Setting.EnablePlateNo;
                    MemberBarcodePrefix = Setting.MemberBarcodePrefix;
                    PrepaidInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Setting.PrepaidInfo);
                    AllowMultiVehicleForStaff = (byte)Setting.AllowMultiVehicleForStaff;
                    TCList = cnmain.Query<PSlipTerms>("SELECT Description, Height from PSlipTerms");
                    FYID = cnmain.ExecuteScalar<byte>("SELECT FYID FROM tblFiscalYear WHERE CONVERT(VARCHAR,GETDATE(),101) BETWEEN BEGIN_DATE AND END_DATE");
                    FYNAME = cnmain.ExecuteScalar<string>("SELECT FYNAME FROM tblFiscalYear WHERE CONVERT(VARCHAR,GETDATE(),101) BETWEEN BEGIN_DATE AND END_DATE");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Parking Management", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void CheckFiscalId(SqlConnection conn)
        {
            FYID = conn.ExecuteScalar<byte>("SELECT FYID FROM tblFiscalYear WHERE CONVERT(VARCHAR,GETDATE(),101) BETWEEN BEGIN_DATE AND END_DATE");
        }

        public static void CheckFiscalId(SqlTransaction tran)
        {
            FYID = tran.Connection.ExecuteScalar<byte>("SELECT FYID FROM tblFiscalYear WHERE CONVERT(VARCHAR,GETDATE(),101) BETWEEN BEGIN_DATE AND END_DATE");
        }

        public static string TConnectionString
        {
            get
            {
                return DataConnectionString;
                //SqlConnectionStringBuilder ConnBuilder = new SqlConnectionStringBuilder(DataConnectionString);
                //ConnBuilder.UserID = User.UserName;
                //ConnBuilder.Password = User.DBPassword;
                //return ConnBuilder.ConnectionString;
            }
        }

        public static string GetTConnectionString(string TUser, string TPassword)
        {
            SqlConnectionStringBuilder ConnBuilder = new SqlConnectionStringBuilder(DataConnectionString);
            ConnBuilder.UserID = TUser;
            ConnBuilder.Password = TPassword;
            return ConnBuilder.ConnectionString;
        }

        internal static string GetNumToWords(SqlConnection conn, decimal GrossAmount)
        {
            string InWords = "Rs. " + conn.ExecuteScalar<string>("SELECT DBO.Num_ToWordsArabic(" + Math.Floor(GrossAmount) + ")");
            if (GrossAmount > Math.Floor(GrossAmount))
            {
                InWords += " & " + conn.ExecuteScalar<string>("SELECT DBO.Num_ToWordsArabic(" + GrossAmount.ToString("#0.00").Split('.')[1] + ")") + " Paisa";
            }

            return InWords;
        }

        internal static void UpdateDatabase(SqlConnection conn)
        {
            conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'UpdateHistory')
                        ALTER TABLE tblSetting ADD UpdateHistory SMALLINT");

            short UpdateHistory = conn.ExecuteScalar<short>("SELECT ISNULL(UpdateHistory,0) FROM tblSetting");

            if (UpdateHistory < 1)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'DisableCashAmountChange')
                        ALTER TABLE tblSetting ADD DisableCashAmountChange BIT");

                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'MinChargeUnit')
                        ALTER TABLE tblSetting ADD MinChargeUnit NUMERIC(9,2) NOT NULL, CONSTRAINT DF_Setting_MinChargeUnit DEFAULT (15) FOR MinChargeUnit");

                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'MinReturnableCurrency')
                        ALTER TABLE tblSetting ADD MinReturnableCurrency NUMERIC(9,2) NOT NULL, CONSTRAINT DF_Setting_MinReturnableCurrency DEFAULT (5) FOR MinReturnableCurrency");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CashReceiptParticulars')
                            BEGIN
                            CREATE TABLE CashReceiptParticulars (Particular VARCHAR(MAX) NOT NULL)
                            INSERT INTO CashReceiptParticulars VALUES ('Slip Lost Penalty')
                            INSERT INTO CashReceiptParticulars VALUES ('Parking Charge')
                            END");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblFiscalYear')
                                BEGIN
                                CREATE TABLE tblFiscalYear (FYID TINYINT NOT NULL, FYNAME VARCHAR(20) NOT NULL, BEGIN_DATE SMALLDATETIME NOT NULL, END_DATE SMALLDATETIME NOT NULL, CONSTRAINT PK_FiscalYear PRIMARY KEY (FYID))
                                INSERT INTO tblFiscalYear VALUES(1, '7273','07/17/2015','07/15/2016')
                                INSERT INTO tblFiscalYear VALUES(2, '7374','07/16/2016','07/15/2017')
                                INSERT INTO tblFiscalYear VALUES(3, '7475','07/16/2017','07/16/2018')
                                INSERT INTO tblFiscalYear VALUES(4, '7576','07/17/2018','07/16/2019')
                                INSERT INTO tblFiscalYear VALUES(5, '7677','07/17/2019','07/15/2020')
                                END");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblSequence')
                                BEGIN
                                CREATE TABLE tblSequence (VNAME VARCHAR(20) NOT NULL, FYID TINYINT NOT NULL, CurNo INT NOT NULL)
                                INSERT INTO tblSequence VALUES('PID', 1, (SELECT MAX(PID) + 1 FROM ParkingInDetails))
                                INSERT INTO tblSequence VALUES('BillNo', 1, (SELECT MAX(BillNo) + 1 FROM ParkingSales))
                                INSERT INTO tblSequence VALUES('SID', 1, (SELECT MAX(SETTLEMENT_ID) + 1 FROM CashSettlement))

                                INSERT INTO tblSequence VALUES('PID', 2, 1)
                                INSERT INTO tblSequence VALUES('BillNo', 2, 1)
                                INSERT INTO tblSequence VALUES('SID', 2, 1)

                                INSERT INTO tblSequence VALUES('PID', 3, 1)
                                INSERT INTO tblSequence VALUES('BillNo', 3, 1)
                                INSERT INTO tblSequence VALUES('SID', 3, 1)

                                INSERT INTO tblSequence VALUES('PID', 4, 1)
                                INSERT INTO tblSequence VALUES('BillNo', 4, 1)
                                INSERT INTO tblSequence VALUES('SID', 4, 1)

                                INSERT INTO tblSequence VALUES('PID', 5, 1)
                                INSERT INTO tblSequence VALUES('BillNo', 5, 1)
                                INSERT INTO tblSequence VALUES('SID', 5, 1)
                                END");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParkingArea' AND COLUMN_NAME = 'MinVacantLot')
                                    ALTER TABLE ParkingArea ADD MinVacantLot SMALLINT NOT NULL CONSTRAINT DF_MinVacantLot DEFAULT (0)");

                conn.Execute(@"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParkingOutDetails' AND Column_Name = 'ChargedHours' AND DATA_TYPE = 'smallint')
                        ALTER TABLE ParkingOutDetails ALTER COLUMN ChargedHours Numeric(9,2)");


                conn.Execute("IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParkingSales' AND COLUMN_NAME = 'BillTo') ALTER TABLE ParkingSales ADD BillTo VARCHAR(255)");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'FYID' AND TABLE_NAME = 'ParkingSales')
                            ALTER TABLE ParkingSales ADD FYID TINYINT NOT NULL, CONSTRAINT DF_ParkingSales_FYID DEFAULT (1) FOR FYID");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'FYID' AND TABLE_NAME = 'ParkingOutDetails')
                            ALTER TABLE ParkingOutDetails ADD FYID TINYINT NOT NULL, CONSTRAINT DF_ParkingOutDetails_FYID DEFAULT (1) FOR FYID");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'FYID' AND TABLE_NAME = 'ParkingInDetails')
                            ALTER TABLE ParkingInDetails ADD FYID TINYINT NOT NULL, CONSTRAINT DF_ParkingInDetails_FYID DEFAULT (1) FOR FYID");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'FYID' AND TABLE_NAME = 'POutMemberDetails')
                            ALTER TABLE POutMemberDetails ADD FYID TINYINT NOT NULL, CONSTRAINT DF_POutMemberDetails_FYID DEFAULT (1) FOR FYID");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WHERE TABLE_NAME = 'ParkingOutDetails' AND CONSTRAINT_NAME = 'FK_ParkingOut_ParkingIn' AND COLUMN_NAME <> 'PID')
                            BEGIN
	                            ALTER TABLE ParkingOutDetails DROP CONSTRAINT FK_ParkingOut_ParkingIn
	                            ALTER TABLE POutMemberDetails DROP CONSTRAINT FK_POutMemberDetails_POUT
	                            ALTER TABLE ParkingInDetails DROP CONSTRAINT PK_ParkingDetails

                                ALTER TABLE ParkingSales ADD CONSTRAINT PK_ParkingSales PRIMARY KEY (BillNo, FYID)
	                            ALTER TABLE ParkingInDetails ADD CONSTRAINT PK_ParkingDetails PRIMARY KEY (PID, FYID)
	                            ALTER TABLE ParkingOutDetails ADD CONSTRAINT FK_ParkingOut_ParkingIn FOREIGN KEY (PID, FYID) REFERENCES ParkingInDetails(PID, FYID)
	                            ALTER TABLE POutMemberDetails ADD CONSTRAINT FK_POutMemberDetails_POUT FOREIGN KEY (PID, FYID) REFERENCES ParkingInDetails(PID, FYID)
                            END ");

                conn.Execute("IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'POUT_ClearLog') SELECT * INTO POUT_ClearLog FROM ParkingOutDetails WHERE 0 = 1");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CashSettlement' AND COLUMN_NAME = 'CollectionAmount')
                                    ALTER TABLE CashSettlement ADD CollectionAmount NUMERIC(18,12) NOT NULL, CONSTRAINT DF_CollectionAmount DEFAULT(0) FOR CollectionAmount");

                conn.Execute("Update tblSetting SET GraceTime = 0, ShowCollectionAmountInCashSettlement = 0");

                conn.Execute
                (
@"ALTER PROCEDURE [dbo].[sp_Calculate_PCharge] 
@Intime datetime,
@OutTime datetime,
@RateID smallint,
@VehicleID tinyint

AS

set Nocount On
Declare @GraceTime smallint
Declare @BeginTime Datetime,@EndTime Datetime,@Rate numeric(18,2),@isFixed numeric,@FixFlag numeric
Declare @CumulativeAmount numeric(18,2)
Declare @ChargedHours as numeric(9,2)
Declare @MinChargeUnit as numeric(9,2)
Declare @MinReturnableCurrency as numeric(9,2)
SELECT  @GraceTime=GraceTime, @MinChargeUnit = MinChargeUnit, @MinReturnableCurrency = MinReturnableCurrency FROM tblSetting
set @CumulativeAmount=0
set @ChargedHours=0
SET DATEFIRST 7
SET @FixFlag = 0
	Begin
		set @BeginTime = @Intime
		While @BeginTime <=@OutTime
		Begin
			if Datediff(n,@BeginTime,@OutTime) > @GraceTime
				Begin
					select @FixFlag = (CASE WHEN Rate = @Rate THEN @FixFlag ELSE 0 END), @Rate=Rate, @isFixed=IsFixed from RateDetails where @BeginTime between  convert(datetime,convert(varchar,@BeginTime,1))+ BeginTime and convert(datetime,convert(varchar,@BeginTime,1))+Endtime and VehicleType=@vehicleID and Rate_Id=@RateID and [Day] = DATEPART(weekday,@BeginTime)
					if @IsFixed=1
						Begin
							if @FixFlag=0
								Begin	
									set @CumulativeAmount=@CumulativeAmount+@Rate
									set @ChargedHours=@ChargedHours+1
								end
							set @FixFlag=1
						End
					else
						Begin
							if Datediff(n,@BeginTime,@OutTime) >= 60
								begin
									set @CumulativeAmount=@CumulativeAmount+@Rate
									set @ChargedHours=@ChargedHours+1
									set @FixFlag=0
								end
							else
								begin
									While @BeginTime <= @OutTime
										begin
											if Datediff(n,@BeginTime,@OutTime) > @GraceTime
												begin
													set @CumulativeAmount=@CumulativeAmount + @Rate * @MinChargeUnit/60
													set @ChargedHours=@ChargedHours + @MinChargeUnit/60
													set @FixFlag=0											
												end
											set @BeginTime= dateadd(n,@MinChargeUnit, @BeginTime)
										end											
									end
						End
				End
			set @BeginTime= dateadd(n,60, @BeginTime)
		End
	End
	select CEILING(@CumulativeAmount/@MinReturnableCurrency) * @MinReturnableCurrency as Rate,@ChargedHours as TotHrs
set Nocount off"
                );

                conn.Execute("UPDATE tblSetting SET UpdateHistory = 1");

                conn.Execute(@"DECLARE @MID INT;
                            SELECT  @MID = MAX(MID) + 1 FROM MENU
                            INSERT INTO MENU VALUES (@MID, 'Cash Receipt', 3, NULL, 'ParkingManagement.Forms.Transaction.Cash_Receipt.ucTouchCashReceipt')
                            INSERT INTO UserRight  SELECT UID, @MID, 1 FROM Users
                            ");
            }
            if (UpdateHistory < 2)
            {
                conn.Execute(@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'SESSION_SETTLED' AND TABLE_NAME = 'SESSION' AND DATA_TYPE = 'bit')
                            BEGIN
                            ALTER TABLE [SESSION] DROP CONSTRAINT DF_SETTLED
                            ALTER TABLE [SESSION] ALTER COLUMN SESSION_SETTLED INT NOT NULL
                            ALTER TABLE [SESSION] ADD CONSTRAINT DF_SETTLED DEFAULT (0) FOR SESSION_SETTLED
                            END ");

                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'SettlementMode')
                                ALTER TABLE tblSetting ADD SettlementMode TINYINT NOT NULL, CONSTRAINT DF_SETTLEMENTMODE DEFAULT (0) FOR SettlementMode");

                conn.Execute("UPDATE tblSetting SET UpdateHistory = 2");
            }
            if (UpdateHistory < 3)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblStaff')
BEGIN
CREATE TABLE tblStaff(BARCODE VARCHAR(200) NOT NULL, FULLNAME VARCHAR(255) NOT NULL, ADDRESS VARCHAR(255) NULL, DESIGNATION VARCHAR(100) NULL, REMARKS VARCHAR(255) NULL, STATUS TINYINT NOT NULL CONSTRAINT PK_STAFF PRIMARY KEY (BARCODE))
ALTER TABLE ParkingOutDetails ADD STAFF_BARCODE VARCHAR(200) NULL, CONSTRAINT FK_POUT_STAFF FOREIGN KEY (STAFF_BARCODE) REFERENCES tblStaff(BARCODE)
END");

                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'AllowMultiVehicleForStaff')
ALTER TABLE tblSetting ADD AllowMultiVehicleForStaff TINYINT NULL");
                conn.Execute("UPDATE tblSetting SET UpdateHistory = 3");
            }
            if (UpdateHistory < 4)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblSyncLog')
CREATE TABLE tblSyncLog(VCHRNO varchar(50) NOT NULL, SYNC_DATE smalldatetime NOT NULL, SYNC_TIME varchar(20) NOT NULL, JSON_DATA nvarchar(max) NULL, [STATUS] tinyint NOT NULL, RETURN_CODE int NOT NULL)");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'IrdApiUser')
ALTER TABLE tblSetting ADD IrdApiUser VARCHAR(200) NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'IrdApiPassword')
ALTER TABLE tblSetting ADD IrdApiPassword VARCHAR(50) NULL");
                conn.Execute(@"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MemberDiscountDetail' AND COLUMN_NAME = 'SkipInterval')
ALTER TABLE MemberDiscountDetail ADD SkipInterval INT NOT NULL, CONSTRAINT DF_MemberDiscountDetail_SkipInterval DEFAULT (0) FOR SkipInterval");
                conn.Execute("UPDATE tblSetting SET UpdateHistory = 4");
            }
            if (UpdateHistory < 5)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'SlipPrinterWidth')
ALTER TABLE tblSetting ADD SlipPrinterWidth TINYINT NOT NULL, CONSTRAINT DF_tblSetting_SlipPrinterWidth DEFAULT (80) FOR SlipPrinterWidth");
                conn.Execute("UPDATE tblSetting SET UpdateHistory = 5");
            }
            if (UpdateHistory < 6)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSyncLog' AND COLUMN_NAME = 'IsRealTime')
ALTER TABLE tblSyncLog ADD IsRealTime TINYINT NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSyncLog' AND COLUMN_NAME = 'FYNAME')
ALTER TABLE tblSyncLog ADD FYNAME VARCHAR(10) NULL");
                conn.Execute("UPDATE tblSetting SET UpdateHistory = 6");
            }
            if (UpdateHistory < 7)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnablePlateNo')
ALTER TABLE tblSetting ADD EnablePlateNo BIT NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnableStaff')
ALTER TABLE tblSetting ADD EnableStaff BIT NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnableStamp')
ALTER TABLE tblSetting ADD EnableStamp BIT NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnableDiscount')
ALTER TABLE tblSetting ADD EnableDiscount BIT NULL");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnableDiscount')
ALTER TABLE tblSetting ADD CalculationMethod TINYINT NULL");

            }

            if (UpdateHistory < 8)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'MemberBarcodePrefix')
ALTER TABLE tblSetting ADD MemberBarcodePrefix varchar(5) NOT NULL, CONSTRAINT DF_tblSetting_MemberBarcodePrefix DEFAULT ('@') FOR MemberBarcodePrefix");
            }
            if (UpdateHistory < 9)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'VoucherTypes' AND COLUMN_NAME = 'NonVat')
ALTER TABLE VoucherTypes ADD NonVat BIT");
            }
            if (UpdateHistory < 10)
            {
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'EnablePrepaid')
ALTER TABLE tblSetting ADD EnablePrepaid BIT");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblSetting' AND COLUMN_NAME = 'PrepaidInfo')
ALTER TABLE tblSetting ADD PrepaidInfo VARCHAR(500)");
                conn.Execute(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DiscountScheme')
CREATE TABLE DiscountScheme (SchemeId INT NOT NULL, SchemeName VARCHAR(100) NOT NULL, ValidOnWeekends BIT NOT NULL, ValidOnHolidays BIT NOT NULL, ValidHours VARCHAR(250) NOT NULL, DiscountPercent Numeric(5,2) NOT NULL, DiscountAmount VARCHAR(250), MinHrs INT NOT NULL, MaxHrs INT NOT NULL, ExpiryDate DATETIME NOT NULL, CONSTRAINT PK_DiscountScheme PRIMARY KEY (SCHEMEID))");
                conn.Execute("ALTER TABLE PARKINGINDETAILS ALTER COLUMN InTime VARCHAR(12) NOT NULL");
                conn.Execute("ALTER TABLE POUT_CLEARLOG ALTER COLUMN OutTime VARCHAR(12) NOT NULL");
                conn.Execute("ALTER TABLE PARKINGOUTDETAILS ALTER COLUMN OutTime VARCHAR(12) NOT NULL");
                conn.Execute("ALTER TABLE ParkingSales ALTER COLUMN TTime VARCHAR(12) NOT NULL");
            }
            conn.Execute("UPDATE tblSetting SET UpdateHistory = 10");
        }

        public static bool StartSession()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(TConnectionString))
                {
                    Session = (int)conn.ExecuteScalar(string.Format
                         (
                             @"INSERT INTO [SESSION] ( SESSION_ID, [START_DATE], TERMINAL_CODE, [UID], SESSION_CREATE_MODE, HostName)
                                OUTPUT INSERTED.SESSION_ID
                                VALUES ((SELECT ISNULL(MAX(SESSION_ID),0) + 1 FROM SESSION), GETDATE(), '{0}', {1}, 'LOGIN', HOST_NAME())", Terminal, User.UID
                         ));
                    return true;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(GetRootException(ex).Message, "Session Start", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public static void EndSession()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(TConnectionString))
                {
                    conn.Execute(string.Format("UPDATE SESSION SET END_DATE = GETDATE() WHERE SESSION_ID = {0}", Session));
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(GetRootException(ex).Message, "Session End", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        public static Exception GetRootException(Exception ex)
        {
            try
            {
                if (ex.InnerException != null)
                {
                    ex = GetRootException(ex);
                }

                return ex;
            }
            catch (Exception exs)
            {
                MessageBox.Show(exs.Message);
                return exs;
            }
        }
        public static string GetEncryptedPWD(string pwd, ref string Salt)
        {

            StringBuilder sBuilder;

            if (string.IsNullOrEmpty(Salt))
            {
                System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
                byte[] saltByte = new byte[8];
                rng.GetNonZeroBytes(saltByte);

                sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < saltByte.Length; i++)
                {
                    sBuilder.Append(saltByte[i].ToString("x2"));
                }

                Salt = sBuilder.ToString();
            }

            System.Security.Cryptography.SHA256CryptoServiceProvider sha = new System.Security.Cryptography.SHA256CryptoServiceProvider();
            //System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.ASCII.GetBytes(pwd + Salt);
            data = sha.ComputeHash(data);

            sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        public static void bindColumns(ref DataTable dtSrc, params string[] columnName)
        {
            for (int i = 0; i < columnName.Length; i++)
            {
                dtSrc.Columns.Add(columnName.GetValue(i).ToString());
            }
        }

        public static void SetUserActivityLog(string FormName, string TrnMode, string WorkDetail = "", string VCRHNO = "", string Remarks = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Execute
                        (string.Format
                            (
                                @"INSERT INTO tblUserWorkingLog(LogID, UserId, UserSessId, FormName, TrnDate, TrnTime, TrnMode, WorkDetail, VchrNo, Remarks)
                                VALUES ((SELECT ISNULL(MAX(LogID),0) + 1 FROM tblUserWorkingLog), '{0}', {1}, '{2}',  CONVERT(VARCHAR,GETDATE(),101), 
                                CONVERT(VARCHAR, GETDATE(), 108), '{3}', '{4}', '{5}','{6}')", User.UserName, Session, FormName, TrnMode, WorkDetail, VCRHNO, Remarks
                            )
                        );
                }
            }
            catch (Exception)
            {

            }
        }

        public static void SetUserActivityLog(SqlTransaction tran, string FormName, string TrnMode, string WorkDetail = "", string VCRHNO = "", string Remarks = "")
        {

            tran.Connection.Execute
                (string.Format
                    (
                        @"INSERT INTO tblUserWorkingLog(LogID,UserId, UserSessId, FormName, TrnDate, TrnTime, TrnMode, WorkDetail, VchrNo, Remarks)
                                VALUES ((SELECT ISNULL(MAX(LogID),0) + 1 FROM tblUserWorkingLog), '{0}', {1}, '{2}',  CONVERT(VARCHAR,GETDATE(),101), 
                                CONVERT(VARCHAR, GETDATE(), 108), '{3}', '{4}', '{5}','{6}')", User.UserName, Session, FormName, TrnMode, WorkDetail, VCRHNO, Remarks
                    ), transaction: tran
                );
        }

        public static string GetInvoiceNo(string VNAME)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                string invoice = conn.ExecuteScalar<string>("SELECT CurNo FROM tblSequence WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID });
                if (string.IsNullOrEmpty(invoice))
                {
                    conn.Execute("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID });
                    invoice = "1";
                }
                return invoice;
            }
        }

        public static string GetInvoiceNo(string VNAME, SqlTransaction tran)
        {
            string invoice = tran.Connection.ExecuteScalar<string>("SELECT CurNo FROM tblSequence WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
            if (string.IsNullOrEmpty(invoice))
            {
                tran.Connection.Execute("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
                invoice = "1";
            }
            return invoice;
        }

        public static string GetReprintCaption(string BillNo)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                string strCaption = string.Format(@"SELECT COUNT(*) + 1 FROM tblPrintLog WHERE BillNo = '{0}' AND FYID = {1}",
                                         BillNo, FYID);
                int RePrintCount = conn.ExecuteScalar<int>(strCaption);
                //if (RePrintCount == 1)
                //    return "Copy of Original";
                //else
                return "Copy of Original (" + RePrintCount + ")";
            }
        }

        public static void SavePrintLog(string BillNo, string PrintRemarks, string PrintDesc)
        {
            string strSavePrintLog;
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                strSavePrintLog = string.Format(@"INSERT INTO tblPrintLog(BillNo, PrintDate, PrintTime, PrintUser, PrintRemarks, PrintDesc, FYID) 
                                            VALUES ('{0}', CONVERT(VARCHAR,GETDATE(),101), CONVERT(VARCHAR, GETDATE(), 108), '{1}', {2}, '{3}', {4})",
                                        BillNo, User.UserName, string.IsNullOrEmpty(PrintRemarks) ? "null" : "'" + PrintRemarks + "'", PrintDesc, FYID);
                conn.Execute(strSavePrintLog);
            }
        }

        static string Encrypt(string Text, string Key = "AmitLalJoshi")
        {
            int i;
            string TEXTCHAR;
            string KEYCHAR;
            string encoded = string.Empty;
            for (i = 0; i < Text.Length; i++)
            {
                TEXTCHAR = Text.Substring(i, 1);
                var keysI = (i % (Key.Length - 1)) + (i < Key.Length - 1 ? 1 : 0);
                KEYCHAR = Key.Substring(keysI, Key.Length - keysI);

                var encrypted = Microsoft.VisualBasic.Strings.Asc(TEXTCHAR) ^ Microsoft.VisualBasic.Strings.Asc(KEYCHAR);
                encoded += Microsoft.VisualBasic.Strings.Chr(encrypted);
            }
            return encoded;
        }
    }
}
