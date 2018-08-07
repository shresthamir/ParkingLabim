using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using RawPrintFunctions;
using Dapper;
using System.Windows.Threading;
namespace ParkingManagement.ViewModel
{
    class CashReceiptViewModel : BaseViewModel
    {
        #region Members

        Decimal _Amount;

        string _Remarks;
        private List<string> _ParticularsList;
        private string _BillTo;
        private string _Particulars;
        string _CurTime;
        DateTime _CurDate;
        DispatcherTimer timer;
        DateConverter nepDate;
        private bool _MustIssueTaxInvoice;
        private string _InvoiceNo;
        private string _InvoicePrefix;
        private bool _TaxInvoice;
        private string _BillToAdd;
        private string _BillToPan;
        #endregion


        #region Properties
        public string BillTo { get { return _BillTo; } set { _BillTo = value; OnPropertyChanged("BillTo"); } }
        public string BillToAdd { get { return _BillToAdd; } set { _BillToAdd = value; OnPropertyChanged("BillToAdd"); } }
        public string BillToPan { get { return _BillToPan; } set { _BillToPan = value; OnPropertyChanged("BillToPan"); } }
        public Decimal Amount { get { return _Amount; } set { _Amount = value; OnPropertyChanged("Amount"); } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; OnPropertyChanged("Remarks"); } }
        public string Particulars { get { return _Particulars; } set { _Particulars = value; OnPropertyChanged("Particulars"); } }
        public List<string> ParticularsList { get { return _ParticularsList; } set { _ParticularsList = value; OnPropertyChanged("ParticularsList"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }

        public bool TaxInvoice
        {
            get { return _TaxInvoice; }
            set
            {
                _TaxInvoice = value;
                OnPropertyChanged("TaxInvoice");
                InvoicePrefix = (value) ? "TI" : "SI";
                if (_action == ButtonAction.New)
                    InvoiceNo = GlobalClass.GetInvoiceNo(InvoicePrefix);
            }
        }
        #endregion

        #region Commands
        public RelayCommand LoadParty { get; set; }
        public RelayCommand LoadData2 { get; set; }

        #endregion

        public CashReceiptViewModel()
        {
            try
            {
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ParticularsList = new List<string>(conn.Query<string>("SELECT Particular FROM CashReceiptParticulars"));
                    CurDate = DateTime.Today;
                    CurTime = DateTime.Now.ToString("hh:mm tt");
                    timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 1);
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
                MessageBoxCaption = "Cash Receipt";
                TaxInvoice = false;
                NewCommand = new RelayCommand(ExecuteNew);
                SaveCommand = new RelayCommand(ExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                this.PropertyChanged += CashReceiptViewModel_PropertyChanged;
                SetAction(ButtonAction.Init);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void CashReceiptViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Amount")
            {
                if (Amount >= 5000)
                {
                    TaxInvoice = true;
                    CanChangeInvoiceType = false;
                }
                else
                {
                    CanChangeInvoiceType = true;
                }
            }
        }

        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            SetAction(ButtonAction.New);
            InvoiceNo = GlobalClass.GetInvoiceNo(InvoicePrefix);
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

        private void ExecuteSave(object obj)
        {
            string strSql;
            string CurMiti;
            string BillNo;
            decimal Taxable, VAT, _Amount, Discount, NonTaxable;
            if (string.IsNullOrEmpty(Particulars))
            {
                MessageBox.Show("Please select particulars first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (Amount <= 0)
            {
                MessageBox.Show("Amount must be greater than Zero", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (MessageBox.Show("You are about to save current transaction. Do you really want to proceed ?", "Save Transaction", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            string Message = string.Empty;
            try
            {
                CurMiti = nepDate.CBSDate(CurDate);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        BillNo = InvoicePrefix + GlobalClass.GetInvoiceNo(InvoicePrefix, tran);
                        _Amount = Amount / (1 + (GlobalClass.VAT / 100));
                        Discount = 0;
                        NonTaxable = 0;
                        Taxable = _Amount - (NonTaxable + Discount);
                        VAT = Taxable * GlobalClass.VAT / 100;

                        string strSQL = string.Format
                               (
                                   @"INSERT INTO ParkingSales (BillNo, TDate, TMiti, TTime, BILLTO, BILLTOADD, BILLTOPAN, Description, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, PID, UID, SESSION_ID, FYID, TaxInvoice, Remarks) 
                                    VALUES (@BillNo, @TDATE, @TMITI, @TTIME, @BILLTO, @BILLTOADD, @BILLTOPAN, 'Parking Charge', @Amount, @Discount, @NonTaxable, @Taxable, @VAT, @GrossAmount, @PID, @UID, @SESSION_ID, @FYID, @TaxInvoice, @Remarks)"
                               );
                        conn.Execute(strSQL, new
                        {
                            BillNo = BillNo,
                            TDATE = CurDate.Date,
                            TMITI = CurMiti,
                            TTIME = CurTime,
                            BILLTO = BillTo,
                            BILLTOADD = BillToAdd,
                            BILLTOPAN = BillToPan,
                            Amount = _Amount,
                            Discount = Discount,
                            NonTaxable = NonTaxable,
                            Taxable = Taxable,
                            VAT = VAT,
                            GrossAmount = Amount,
                            PID = 0,
                            UID = GlobalClass.User.UID,
                            SESSION_ID = GlobalClass.Session,
                            FYID = GlobalClass.FYID,
                            TaxInvoice = TaxInvoice,
                            Remarks = Remarks
                        }, transaction: tran);
                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);                        
                        tran.Commit();
                        ExecuteUndo(null);
                    }
                    RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)27).ToString() + ((char)112).ToString() + ((char)0).ToString() + ((char)64).ToString() + ((char)240).ToString(), "Receipt");
                    PrintBill(BillNo.ToString(), conn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExecuteUndo(object obj)
        {
            CanChangeInvoiceType = true;
            TaxInvoice = false;
            BillTo = Remarks = string.Empty;
            Particulars = null;
            Amount = 0;
            SetAction(ButtonAction.Init);
        }



        void PrintBill(string BillNo, SqlConnection conn)
        {
            DataRow dr;
            //// RawPrinterHelper printer = new RawPrinterHelper();

            using (DataAccess da = new DataAccess())
            {
                dr = da.getData(string.Format(@"SELECT PS.*,VT.Description VType,ISNULL(PIN.PlateNo,'') PlateNo,PIN.InTime,PIN.InMiti,POUT.OutTime,POUT.OutMiti,U.UserName, POUT.Interval, POUT.ChargedHours FROM ParkingSales PS 
                                    INNER JOIN Users U ON U.UID = PS.UID
                                    LEFT JOIN ParkingOutDetails POUT  ON PS.PID = POUT.PID AND PS.FYID = POUT.FYID
                                    LEFT JOIN (ParkingInDetails PIN   
                                    LEFT JOIN VehicleType VT ON VT.VTypeID=PIN.VehicleType) ON PS.PID = PIN.PID AND PS.FYID = PIN.FYID
                                    WHERE BillNo = '{0}' AND PS.FYID = {1}", BillNo, GlobalClass.FYID), conn).Rows[0];

            }

            string strPrint = string.Empty;
            int PrintLen = 40;
            string Description = dr["Description"].ToString();
            string Particulars = "Particulars";
            Description = (Description.Length > PrintLen - 17) ? Description.Substring(0, PrintLen - 17) : Description.PadRight(PrintLen - 17, ' ');
            Particulars = (Particulars.Length > PrintLen - 17) ? Particulars.Substring(0, PrintLen - 17) : Particulars.PadLeft((PrintLen + Particulars.Length - 17) / 2, ' ').PadRight(PrintLen - 17, ' ');

            strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
            strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
            strPrint += "Parking Invoice".PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
            strPrint += string.Format("Bill No : {0}    Date : {1}", BillNo.PadRight(7, ' '), dr["TMiti"]) + Environment.NewLine;
            strPrint += string.Format("Received From : {0}", dr["BillTo"]) + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Sn.|{0}|  Amount  |", Particulars) + Environment.NewLine;
            strPrint += string.Format("1.   {0}  {1}", Description, GParse.ToDecimal(dr["GrossAmount"]).ToString("#0.00").PadLeft(8, ' ')) + Environment.NewLine;
            //strPrint += string.Format("     IN  : {0} {1}", dr["InTime"], dr["InMiti"]) + Environment.NewLine;
            //strPrint += string.Format("     OUT : {0} {1}", dr["OutTime"], dr["OutMiti"]) + Environment.NewLine;
            //strPrint += string.Format("     Interval : {0} ", dr["Interval"]) + Environment.NewLine;
            //strPrint += string.Format("     Charged Hours : {0} ", dr["ChargedHours"]) + Environment.NewLine;

            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
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
        void PrintBill(int BillNo)
        {
            DataRow dr;
            //// RawPrinterHelper printer = new RawPrinterHelper();
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            using (DataAccess da = new DataAccess())
            {
                conn.Open();
                dr = da.getData(string.Format(@"SELECT PS.*,VT.Description VType,ISNULL(PIN.PlateNo,'') PlateNo,PIN.InTime,PIN.InMiti,POUT.OutTime,POUT.OutMiti,U.UserName, POUT.Interval, POUT.ChargedHours FROM ParkingSales PS 
                                    INNER JOIN Users U ON U.UID = PS.UID
                                    LEFT JOIN ParkingOutDetails POUT  ON PS.PID = POUT.PID AND PS.FYID = POUT.FYID
                                    LEFT JOIN (ParkingInDetails PIN   
                                    LEFT JOIN VehicleType VT ON VT.VTypeID=PIN.VehicleType) ON PS.PID = PIN.PID AND PS.FYID = PIN.FYID
                                    WHERE BillNo = {0} AND PS.FYID = {1}", BillNo, GlobalClass.FYID), conn).Rows[0];
            }
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "           " + GlobalClass.CompanyName + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "                    " + GlobalClass.CompanyAddress + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "                Parking Invoice" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "Bill No : " + (BillNo + "          ").Substring(0, 10) + "        Date : " + dr["TMiti"] + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "------------------------------------------------" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "Sno. |         Particulars          |  Amount  |" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "------------------------------------------------" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "1.     " + dr["Description"] + "                    " + GParse.ToDecimal(dr["GrossAmount"]).ToString("#0.00") + "  " + Environment.NewLine, "Receipt");
            // RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "       IN  : " + dr["InTime"] + " " + dr["InMiti"] + "                " + Environment.NewLine, "Receipt");
            //RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "       OUT : " + dr["OutTime"] + " " + dr["OutMiti"] + "                " + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "------------------------------------------------" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "Cashier : " + dr["UserName"] + "(" + dr["TTime"] + ")" + Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, Environment.NewLine, "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, "------------------------------------------------", "Receipt");
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)29).ToString() + ((char)86).ToString() + ((char)1).ToString(), "Receipt");
        }
    }
}
