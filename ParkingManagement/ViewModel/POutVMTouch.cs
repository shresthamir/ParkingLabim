using DateFunction;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using RawPrintFunctions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Dapper;
using System.Drawing.Printing;
using System.Drawing;
using System.Drawing.Drawing2D;
using ParkingManagement.Forms.Transaction;
namespace ParkingManagement.ViewModel
{
    public class POutVMTouch : BaseViewModel
    {
        enum Focusable
        {
            Barcode = 0, CashAmount = 1
        }
        DateConverter nepDate;
        bool _PartyEnabled;
        DispatcherTimer timer;
        wStaffBarcode StaffBarcode;
        ParkingIn _PIN;
        ParkingOut _POUT;
        ObservableCollection<RateMaster> _RSchemes;
        string _CurTime;
        DateTime _CurDate;
        private string _InvoicePrefix;
        private string _InvoiceNo;
        private bool _TaxInvoice;
        private bool _MustIssueTaxInvoice;
        public ParkingIn PIN { get { return _PIN; } set { _PIN = value; OnPropertyChanged("PIN"); } }
        public ParkingOut POUT { get { return _POUT; } set { _POUT = value; OnPropertyChanged("POUT"); } }
        public ObservableCollection<RateMaster> RSchemes
        {
            get { return _RSchemes; }
            set { _RSchemes = value; OnPropertyChanged("RSchemes"); }
        }
        public bool TaxInvoice
        {
            get { return _TaxInvoice; }
            set
            {
                _TaxInvoice = value;
                OnPropertyChanged("TaxInvoice");
                InvoicePrefix = (value) ? "TI" : "SI";
                if (_action != ButtonAction.RePrint)
                    InvoiceNo = GlobalClass.GetInvoiceNo(InvoicePrefix);
            }
        }
        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public bool PartyEnabled { get { return _PartyEnabled; } set { _PartyEnabled = value; OnPropertyChanged("PartyEnabled"); } }

        public RelayCommand PrintCommand { get; set; }
        public RelayCommand OpenStaffBarcodeCommand { get; set; }
        public RelayCommand SaveWithStaffCommand { get; set; }
        public RelayCommand RePrintCommand { get { return new RelayCommand(ExecuteRePrint, CanExecuteRePrint); } }
        public RelayCommand LoadInvoice { get { return new RelayCommand(ExecuteLoadInvoice, CanLoadInvoice); } }

        private void ExecuteLoadInvoice(object obj)
        {
            string BillNo = InvoicePrefix + obj.ToString();
            int PID;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new {BillNo = BillNo, FYID = GlobalClass.FYID});
                    //if(PID == 0)
                    //{
                    //    return;
                    //}
                    PIN = conn.Query<ParkingIn>("SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode FROM ParkingInDetails WHERE PID = @PID AND FYID = @FYID", new { PID = PID, FYID = GlobalClass.FYID }).First();
                    PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                    POUT = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID)).First();
                    SetAction(ButtonAction.InvoiceLoaded);
                }
            }
            catch (Exception)
            {
                                
            }
        }

        private bool CanLoadInvoice(object obj)
        {
            return _action == ButtonAction.RePrint;
        }
        private bool CanExecuteRePrint(object obj)
        {
            return _action == ButtonAction.Init;
        }

        private void ExecuteRePrint(object obj)
        {            
            SetAction(ButtonAction.RePrint);
            OnPropertyChanged("IsEntryMode");
        }
        public POutVMTouch()
        {
            try
            {
                MessageBoxCaption = "Parking Out";
                TaxInvoice = false;
                CanChangeInvoiceType = true;
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                PIN = new ParkingIn();
                POUT = new ParkingOut();
                RSchemes = new ObservableCollection<RateMaster>();

                CurDate = DateTime.Today;
                CurTime = DateTime.Now.ToString("hh:mm tt");
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;
                timer.Start();
                LoadRateSchemes();
                LoadData = new RelayCommand(ExecuteLoad);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                OpenStaffBarcodeCommand = new RelayCommand(OpenStaffBarcode, CanExecuteSave);
                SaveWithStaffCommand = new RelayCommand(SaveWithStaff);
                POUT.PropertyChanged += POUT_PropertyChanged;
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecutePrint(object obj)
        {
            return _action == ButtonAction.InvoiceLoaded;
        }

        private void SaveWithStaff(object obj)
        {
            try
            {
                if (obj == null)
                {
                    MessageBox.Show("Invalid Barcode");
                }
                POUT.STAFF_BARCODE = obj.ToString();
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM tblStaff WHERE STATUS = 0 AND BARCODE = '" + POUT.STAFF_BARCODE + "'") == 0)
                    {
                        MessageBox.Show("Invalid Barcode. Please Try Again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    if (GlobalClass.AllowMultiVehicleForStaff == 0)
                    {
                        if (conn.ExecuteScalar<int>
                        (
                            string.Format
                            (
                                @"SELECT COUNT(*) FROM
                            (
                                SELECT  INDATE + CAST(INTIME AS TIME) INTIME, OUTDATE + CAST(OUTTIME AS TIME) OUTTIME FROM ParkingInDetails PID 
                                JOIN ParkingOutDetails POD ON PID.PID =POD.PID AND PID.FYID = POD.FYID WHERE POD.STAFF_BARCODE = '{0}'
                            ) A WHERE (INTIME < '{1}' AND OUTTIME > '{1}') OR (INTIME > '{1}' AND OUTTIME > '{1}')", POUT.STAFF_BARCODE, PIN.InDate.Add(DateTime.Parse(PIN.InTime).TimeOfDay)
                            )
                        ) > 0)
                        {
                            MessageBox.Show("Staff already parked one vehicle during current vehile's parked period. Staff are not allowed to park multiple vehicle at a time", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                    }
                }
                POUT.CashAmount = 0;
                ExecuteSave(null);
                StaffBarcode.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                POUT.STAFF_BARCODE = null;
            }
        }

        private void OpenStaffBarcode(object obj)
        {
            StaffBarcode = new wStaffBarcode() { DataContext = this };
            StaffBarcode.ShowDialog();
        }

        private void ExecutePrint(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {      
                    string BillNo = InvoicePrefix + InvoiceNo;
                    string DuplicateCaption = GlobalClass.GetReprintCaption(BillNo);
                    PrintBill(BillNo, conn, (TaxInvoice) ? "INVOICE" : "ABBREVIATED TAX INVOCE", DuplicateCaption);
                    GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                    GlobalClass.SetUserActivityLog("Parking Out", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                }
                ExecuteUndo(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave(object obj)
        {
            return _action == ButtonAction.Selected;
        }
        private void CalculateAmount()
        {
            int IntervalMins, TotIntervalMins;
            List<RateDetails> RDetails;
            try
            {
                POUT.ChargedAmount = 0;
                POUT.CashAmount = 0;
                POUT.RoyaltyAmount = 0;
                IntervalMins = getMinutes(POUT.Interval);
                TotIntervalMins = IntervalMins;
                RDetails = new List<RateDetails>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteLoad(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (_action == ButtonAction.Selected)
                    {
                        POUT.SaveLog(conn);
                    }

                    var PINS = conn.Query<ParkingIn>(string.Format("SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode, UID FROM ParkingInDetails WHERE BARCODE = '{0}' AND FYID = {1}", obj, GlobalClass.FYID));
                    if (PINS.Count() <= 0)
                    {
                        MessageBox.Show("Invalid barcode readings.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        PIN.Barcode = string.Empty;
                        return;
                    }
                    PIN = PINS.First();
                    PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                    var POUTS = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID));
                    if (POUTS.Count() > 0)
                    {
                        POUT = POUTS.First();
                        MessageBox.Show("Vehicle already exited", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        PIN.Barcode = string.Empty;
                        return;
                    }
                    POUT.Rate_ID = (int)conn.ExecuteScalar("SELECT RATE_ID FROM RATEMASTER WHERE IsDefault = 1");


                    POUT.OutDate = _CurDate;
                    POUT.OutTime = _CurTime;
                    POUT.OutMiti = nepDate.CBSDate(POUT.OutDate);
                    POUT.Interval = GetInterval(PIN.InDate, POUT.OutDate, PIN.InTime, POUT.OutTime);
                    POUT.PID = PIN.PID;

                    CalculateParkingCharge(conn, PIN.InDate.Add(DateTime.Parse(PIN.InTime).TimeOfDay), POUT.OutDate.Add(DateTime.Parse(POUT.OutTime).TimeOfDay), POUT.Rate_ID, PIN.VehicleType);
                    POUT.CashAmount = POUT.ChargedAmount;
                    SetAction(ButtonAction.Selected);
                    if (POUT.CashAmount > GlobalClass.AbbTaxInvoiceLimit)
                    {
                        TaxInvoice = true;
                        CanChangeInvoiceType = false;
                    }
                    else
                    {
                        CanChangeInvoiceType = true;
                    }
                    FocusedElement = (short)Focusable.CashAmount;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void CalculateParkingCharge(SqlConnection conn, DateTime InTime, DateTime OutTime, int RateId, int VehicleID)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "sp_Calculate_PCharge";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InTime", InTime);
                cmd.Parameters.AddWithValue("@OutTime", OutTime);
                cmd.Parameters.AddWithValue("@RateId", RateId);
                cmd.Parameters.AddWithValue("@VehicleId", VehicleID);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        POUT.ChargedAmount = GParse.ToDecimal(dr[0]);
                        POUT.ChargedHours = (decimal)dr[1];
                    }
                }
            }
        }

       
        private void ExecuteSave(object obj)
        {
            string strSQL;
            decimal Taxable, VAT, Amount, Discount, NonTaxable;
            string BillNo = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        POUT.Save(tran);
                        if (POUT.CashAmount > 0)
                        {
                            BillNo = InvoicePrefix + GlobalClass.GetInvoiceNo(InvoicePrefix, tran);
                            Amount = POUT.CashAmount / (1 + (GlobalClass.VAT / 100));
                            Discount = 0;
                            NonTaxable = 0;
                            Taxable = Amount - (NonTaxable + Discount);
                            VAT = Taxable * GlobalClass.VAT / 100;
                            strSQL = string.Format
                                (
                                    @"INSERT INTO ParkingSales (BillNo, TDate, TMiti, TTime, BILLTO, BILLTOADD, BILLTOPAN, Description, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, PID, UID, SESSION_ID, FYID, TaxInvoice) 
                                    VALUES (@BillNo, @TDATE, @TMITI, @TTIME, @BILLTO, @BILLTOADD, @BILLTOPAN, 'Parking Charge', @Amount, @Discount, @NonTaxable, @Taxable, @VAT, @GrossAmount, @PID, @UID, @SESSION_ID, @FYID, @TaxInvoice)"
                                );
                            conn.Execute(strSQL, new
                            {
                                BillNo = BillNo,
                                TDATE = POUT.OutDate,
                                TMITI = POUT.OutMiti,
                                TTIME = POUT.OutTime,
                                BILLTO = POUT.BILLTO,
                                BILLTOADD = POUT.BILLTOADD,
                                BILLTOPAN = POUT.BILLTOPAN,
                                Amount = Amount,
                                Discount = Discount,
                                NonTaxable = NonTaxable,
                                Taxable = Taxable,
                                VAT = VAT,
                                GrossAmount = POUT.CashAmount,
                                PID = POUT.PID,
                                UID = POUT.UID,
                                SESSION_ID = POUT.SESSION_ID,
                                FYID = GlobalClass.FYID,
                                TaxInvoice = TaxInvoice
                            }, transaction: tran);
                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                            GlobalClass.SetUserActivityLog(tran, "Parking Out", "New", VCRHNO: BillNo, WorkDetail: "Bill No : " + BillNo);
                        }
                        tran.Commit();
                    }
                    if (!string.IsNullOrEmpty(BillNo))
                    {
                        RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)27).ToString() + ((char)112).ToString() + ((char)0).ToString() + ((char)64).ToString() + ((char)240).ToString(), "Receipt");   //Open Cash Drawer
                        PrintBill(BillNo.ToString(), conn, (TaxInvoice) ? "TAX INVOICE" : "ABBREVIATED TAX INVOCE");
                        if (TaxInvoice)
                        {
                            PrintBill(BillNo.ToString(), conn, "INVOICE");
                        }
                    }
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteUndo(object obj)
        {
            if (_action == ButtonAction.Selected)
            {
                if (POUT.ChargedHours > 0)
                {
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        POUT.SaveLog(conn);
                    }
                }
            }
            FocusedElement = (short)Focusable.Barcode;
            TaxInvoice = false;
            CanChangeInvoiceType = true;
            PIN = new ParkingIn();
            POUT = new ParkingOut();
            POUT.PropertyChanged += POUT_PropertyChanged;            
            SetAction(ButtonAction.Init);
            OnPropertyChanged("IsEntryMode");
        }

        string GetInterval(DateTime In, DateTime Out, string InTime, string OutTime)
        {
            var InDate = In.Add(DateTime.Parse(InTime).TimeOfDay);
            var OutDate = Out.Add(DateTime.Parse(OutTime).TimeOfDay);
            var interval = OutDate - InDate;
            return (interval.Days * 24 + interval.Hours).ToString() + " Hrs " + (interval.Minutes).ToString() + " Mins";
        }

        int getMinutes(string duration)
        {
            string[] hrsMins;
            int hrs = 0, min = 0, TotMins;
            if (string.IsNullOrEmpty(duration) || duration == "N/A")
                return 0;
            try
            {
                if (duration.Contains(" "))
                {
                    hrsMins = duration.Split(' ');
                    if (hrsMins.Length == 2)
                    {
                        hrs = GParse.ToInteger(hrsMins[0].Replace("Hrs", string.Empty));
                        min = GParse.ToInteger(hrsMins[1].Replace("Min", string.Empty));
                    }
                    else if (hrsMins.Length == 4)
                    {
                        hrs = GParse.ToInteger(hrsMins[0]);
                        min = GParse.ToInteger(hrsMins[2]);
                    }
                }
                else if (duration.Contains("Hrs"))
                {
                    hrs = GParse.ToInteger(duration.Replace("Hrs", string.Empty));
                    min = 0;
                }
                else
                {
                    hrs = 0;
                    min = GParse.ToInteger(duration.Replace("Min", string.Empty));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            TotMins = hrs * 60 + min;
            return TotMins;

        }

        void LoadRateSchemes()
        {

            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                RSchemes = new ObservableCollection<RateMaster>(conn.Query<RateMaster>("SELECT Rate_ID, RateDescription, IsDefault, [UID] FROM RATEMASTER"));
            }
            GlobalClass.DefaultRate = RSchemes.First(x => x.IsDefault);
        }


        public static void PrintBill(string BillNo, SqlConnection conn, string InvoiceName, string DuplicateCaption = "")
        {
            DataRow dr;
            
            //// RawPrinterHelper printer = new RawPrinterHelper();

            using (DataAccess da = new DataAccess())
            {
                dr = da.getData(string.Format(@"SELECT PS.*,VT.Description VType,ISNULL(PIN.PlateNo,'') PlateNo,PIN.InTime,PIN.InMiti,POUT.OutTime,POUT.OutMiti,U.UserName, POUT.Interval, POUT.ChargedHours FROM ParkingSales PS 
                                    INNER JOIN Users U ON U.UID=PS.UID
                                    LEFT JOIN ParkingOutDetails POUT  ON PS.PID = POUT.PID AND PS.FYID = POUT.FYID
                                    LEFT JOIN (ParkingInDetails PIN   
                                    LEFT JOIN VehicleType VT ON VT.VTypeID=PIN.VehicleType) ON PS.PID = PIN.PID AND PS.FYID = PIN.FYID
                                    WHERE BillNo = '{0}' AND PS.FYID = {1}", BillNo, GlobalClass.FYID), conn).Rows[0];

            }

            string InWords = "Rs. " + conn.ExecuteScalar<string>("SELECT DBO.Num_ToWordsArabic(" + dr["GrossAmount"] + ")");
            string strPrint = string.Empty;
            int PrintLen = 40;
            string Description = dr["Description"].ToString();
            string Particulars = "Particulars";
            string PAN = "PAN : " + GlobalClass.CompanyPan;
            Description = (Description.Length > PrintLen - 17) ? Description.Substring(0, PrintLen - 17) : Description.PadRight(PrintLen - 17, ' ');
            Particulars = (Particulars.Length > PrintLen - 17) ? Particulars.Substring(0, PrintLen - 17) : Particulars.PadLeft((PrintLen + Particulars.Length - 17) / 2, ' ').PadRight(PrintLen - 17, ' ');

            strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
            strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
            strPrint += PAN.PadLeft((PrintLen + PAN.Length) / 2, ' ') + Environment.NewLine;
            strPrint += InvoiceName.PadLeft((PrintLen + InvoiceName.Length) / 2, ' ') + Environment.NewLine;
            if (!string.IsNullOrEmpty(DuplicateCaption))
                strPrint += DuplicateCaption.PadLeft((PrintLen + DuplicateCaption.Length) / 2, ' ') + Environment.NewLine;
            strPrint += string.Format("Bill No : {0}    Date : {1}", BillNo.PadRight(7, ' '), dr["TMiti"]) + Environment.NewLine;
            strPrint += string.Format("Vehicle Type : {0} {1}", dr["VType"], string.IsNullOrEmpty(dr["PlateNo"].ToString()) ? string.Empty : "(" + dr["PlateNo"] + ")") + Environment.NewLine;
            strPrint += string.Format("Name    : {0}", dr["BillTo"]) + Environment.NewLine;
            strPrint += string.Format("Address : {0}", dr["BillToAdd"]) + Environment.NewLine;
            strPrint += string.Format("PAN     : {0}", dr["BillToPan"]) + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Sn.|{0}|  Amount  |", Particulars) + Environment.NewLine;
            strPrint += string.Format("1.  {0}  {1}", Description, GParse.ToDecimal(((bool)dr["TaxInvoice"]) ? dr["Amount"] : dr["GrossAmount"]).ToString("#0.00").PadLeft(8, ' ')) + Environment.NewLine;
            strPrint += string.Format("    IN  : {0} {1}", dr["InTime"], dr["InMiti"]) + Environment.NewLine;
            strPrint += string.Format("    OUT : {0} {1}", dr["OutTime"], dr["OutMiti"]) + Environment.NewLine;
            strPrint += string.Format("    Interval : {0} ", dr["Interval"]) + Environment.NewLine;
            strPrint += string.Format("    Charged Hours : {0} ", dr["ChargedHours"]) + Environment.NewLine;

            strPrint += Environment.NewLine;
            strPrint += "------------------------".PadLeft(PrintLen, ' ') + Environment.NewLine;
            if ((bool)dr["TaxInvoice"])
            {
                strPrint += ("Gross Amount : " + GParse.ToDecimal(dr["Amount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                if (GParse.ToDecimal(dr["Discount"]) > 0)
                    strPrint += ("Discount : " + GParse.ToDecimal(dr["Discount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                strPrint += ("Taxable : " + GParse.ToDecimal(dr["Taxable"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                strPrint += ("Non Taxable : " + GParse.ToDecimal(dr["NonTaxable"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                strPrint += ("VAT 13% : " + GParse.ToDecimal(dr["VAT"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
            }
            strPrint += ("Net Amount : " + GParse.ToDecimal(dr["GrossAmount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ');
            strPrint += Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += InWords + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Cashier : {0} ({1})", dr["UserName"], dr["TTime"]) + Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += ((char)29).ToString() + ((char)86).ToString() + ((char)1).ToString();

            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Receipt");
        }


        void POUT_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Rate_ID")
            {
                CalculateAmount();
                POUT.ChargedAmount = POUT.CashAmount;
            }
        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (CurTime != DateTime.Now.ToString("hh:mm tt"))
            {
                if (DateTime.Now.Second > 5)
                    timer.Interval = new TimeSpan(0, 0, 1);
                else
                    timer.Interval = new TimeSpan(0, 1, 0);
            }
            CurTime = DateTime.Now.ToString("hh:mm tt");
            CurDate = DateTime.Today;
        }
    }

}
