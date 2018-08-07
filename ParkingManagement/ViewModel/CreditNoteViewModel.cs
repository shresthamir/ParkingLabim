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
    public class CreditNoteViewModel : BaseViewModel
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
        private string _RefBillNo;
        private string _Remarks;
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
            }
        }
        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public string RefBillNo { get { return _RefBillNo; } set { _RefBillNo = value; OnPropertyChanged("RefBillNo"); } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; OnPropertyChanged("Remarks"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public bool PartyEnabled { get { return _PartyEnabled; } set { _PartyEnabled = value; OnPropertyChanged("PartyEnabled"); } }

        public RelayCommand PrintCommand { get; set; }
        public RelayCommand LoadInvoice { get { return new RelayCommand(ExecuteLoadInvoice, CanLoadInvoice); } }

        private void ExecuteLoadInvoice(object obj)
        {
            string BillNo = InvoicePrefix + obj.ToString();
            int PID;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingSales WHERE RefBillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }) > 0)
                    {
                        MessageBox.Show("Credit Note has already been issued to selected Bill.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                    if (PID == 0)
                        return;
                    PIN = conn.Query<ParkingIn>("SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode FROM ParkingInDetails WHERE PID = @PID AND FYID = @FYID", new { PID = PID, FYID = GlobalClass.FYID }).First();
                    PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                    POUT = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID)).First();

                }
            }
            catch (Exception)
            {

            }
        }

        private bool CanLoadInvoice(object obj)
        {
            return true;
        }
        public CreditNoteViewModel()
        {
            try
            {
                MessageBoxCaption = "Credit Note";
                TaxInvoice = false;
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                PIN = new ParkingIn();
                POUT = new ParkingOut();
                CurDate = DateTime.Today;
                CurTime = DateTime.Now.ToString("hh:mm tt");
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;
                timer.Start();
                NewCommand = new RelayCommand(ExeucteNew);
                LoadData = new RelayCommand(ExecuteLoad);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                SetAction(ButtonAction.Init);
                this.PropertyChanged += CreditNoteViewModel_PropertyChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void CreditNoteViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_action == ButtonAction.New)
            {
                if (e.PropertyName == "RefBillNo" || e.PropertyName == "TaxInvoice")
                {
                    PIN = new ParkingIn();
                    POUT = new ParkingOut();
                }
            }
        }

        private void ExeucteNew(object obj)
        {
            ExecuteUndo(null);
            SetAction(ButtonAction.New);
            InvoiceNo = GlobalClass.GetInvoiceNo("CN");
        }

        private bool CanExecutePrint(object obj)
        {
            return _action == ButtonAction.InvoiceLoaded;
        }


        private void ExecutePrint(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string CNO = "CN" + InvoiceNo;
                    string DuplicateCaption = GlobalClass.GetReprintCaption(CNO);
                    PrintBill(CNO, conn, "CREDIT NOTE", DuplicateCaption);
                    GlobalClass.SavePrintLog(CNO, null, DuplicateCaption);
                    GlobalClass.SetUserActivityLog("Credit Note", "Re-Print", WorkDetail: string.Empty, VCRHNO: CNO, Remarks: "Reprinted : " + DuplicateCaption);
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
            return true;
        }
        private void ExecuteLoad(object obj)
        {
            string CreditNoteNo = "CN" + obj.ToString();
            int PID;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    var CNote = conn.Query("SELECT * FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = CreditNoteNo, FYID = GlobalClass.FYID });
                    if (CNote.Count() > 0)
                    {
                        string BillNo = CNote.First().RefBillNo;
                        Remarks = CNote.First().Remarks;
                        if (BillNo.Contains("TI"))
                        {
                            TaxInvoice = true;
                        }
                        RefBillNo = BillNo.Substring(2, BillNo.Length - 2);
                        PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                        if (PID == 0)
                        {
                            return;
                        }
                        PIN = conn.Query<ParkingIn>("SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode FROM ParkingInDetails WHERE PID = @PID AND FYID = @FYID", new { PID = PID, FYID = GlobalClass.FYID }).First();
                        PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                        POUT = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID)).First();
                        SetAction(ButtonAction.InvoiceLoaded);
                    }
                    else
                    {
                        MessageBox.Show("Invalid Credit Note No. Please Enter valid Credit Note No.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ExecuteSave(object obj)
        {
            string strSQL;
            string BillNo = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(Remarks))
                {
                    MessageBox.Show("Cannot issue Credit Note without Remarks. Please enter Remarks and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingSales WHERE RefBillNo = @RefBillNo", new { RefBillNo = InvoicePrefix + RefBillNo }, tran) > 0)
                        {
                            MessageBox.Show("Credit Note has already been issued on selected bill. Please enter another Bill No and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        BillNo = "CN" + GlobalClass.GetInvoiceNo("CN", tran);
                        strSQL = string.Format
                            (
                                @"INSERT INTO ParkingSales (BillNo, TDate, TMiti, TTime, BILLTO, BILLTOADD, BILLTOPAN, Description, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, PID, UID, SESSION_ID, FYID, TaxInvoice, RefBillNo, Remarks) 
                                    SELECT @BillNo, @TDATE, @TMITI, @TTIME, BILLTO, BILLTOADD, BILLTOPAN, Description, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, PID, @UID, @SESSION_ID, @FYID, TaxInvoice, @RefBillNo, @Remarks FROM ParkingSales WHERE BillNo = @RefBillNo AND FYID = @FYID"
                            );
                        int res = conn.Execute(strSQL, new
                        {
                            BillNo = BillNo,
                            TDATE = CurDate,
                            TMITI = nepDate.CBSDate(CurDate),
                            TTIME = CurTime,
                            UID = GlobalClass.User.UID,
                            SESSION_ID = GlobalClass.Session,
                            FYID = GlobalClass.FYID,
                            RefBillNo = InvoicePrefix + RefBillNo,
                            Remarks = Remarks
                        }, transaction: tran);


                        if (res > 0)
                        {
                            string strSqlDetails = string.Format
                            (
                                @"INSERT INTO ParkingSalesDetails (BillNo, FYID, PType, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks) 
                                    SELECT @BillNo, @FYID, PType, ProdId, Description, Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @RefBillNo AND FYID = @FYID"
                            );
                            conn.Execute(strSqlDetails, new { BillNo = BillNo, FYID = GlobalClass.FYID, RefBillNo = InvoicePrefix + RefBillNo }, tran);
                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = "CN", FYID = GlobalClass.FYID }, transaction: tran);
                            GlobalClass.SetUserActivityLog("Credit Note", "New", VCRHNO: BillNo);
                            SyncFunctions.LogSyncStatus(tran, BillNo, GlobalClass.FYNAME);
                            MessageBox.Show("Credit Note successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        tran.Commit();
                        if (!string.IsNullOrEmpty(SyncFunctions.username))
                        {
                            SyncFunctions.SyncSalesReturnData(SyncFunctions.getBillReturnObject(BillNo), 1);
                        }
                    }
                    if (!string.IsNullOrEmpty(BillNo))
                    {
                        RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)27).ToString() + ((char)112).ToString() + ((char)0).ToString() + ((char)64).ToString() + ((char)240).ToString(), "Receipt");   //Open Cash Drawer
                        PrintBill(BillNo.ToString(), conn, "CREDIT NOTE");
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
            InvoiceNo = string.Empty;
            RefBillNo = string.Empty;
            Remarks = string.Empty;
            TaxInvoice = false;
            PIN = new ParkingIn();
            POUT = new ParkingOut();
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




        void PrintBill(string BillNo, SqlConnection conn, string InvoiceName, string DuplicateCaption = "")
        {
            DataRow dr;
            //string DuplicateCaption = GlobalClass.GetReprintCaption(BillNo);
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
            string InWords = GlobalClass.GetNumToWords(conn, Convert.ToDecimal(dr["GrossAmount"]));
            string strPrint = string.Empty;
            int PrintLen = 40;
            string Description = dr["Description"].ToString();
            string Particulars = "Particulars";
            Description = (Description.Length > PrintLen - 17) ? Description.Substring(0, PrintLen - 17) : Description.PadRight(PrintLen - 17, ' ');
            Particulars = (Particulars.Length > PrintLen - 17) ? Particulars.Substring(0, PrintLen - 17) : Particulars.PadLeft((PrintLen + Particulars.Length - 17) / 2, ' ').PadRight(PrintLen - 17, ' ');

            strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
            strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
            strPrint += GlobalClass.CompanyPan.PadLeft((PrintLen + GlobalClass.CompanyPan.Length) / 2, ' ') + Environment.NewLine;
            strPrint += InvoiceName.PadLeft((PrintLen + InvoiceName.Length) / 2, ' ') + Environment.NewLine;
            if (!string.IsNullOrEmpty(DuplicateCaption))
                strPrint += DuplicateCaption.PadLeft((PrintLen + DuplicateCaption.Length) / 2, ' ') + Environment.NewLine;
            strPrint += string.Format("Bill No : {0}    Date : {1}", BillNo.PadRight(7, ' '), dr["TMiti"]) + Environment.NewLine;
            strPrint += string.Format("Name    : {0}", dr["BillTo"]) + Environment.NewLine;
            strPrint += string.Format("Address : {0}", dr["BillToAdd"]) + Environment.NewLine;
            strPrint += string.Format("PAN     : {0}", dr["BillToPan"]) + Environment.NewLine;
            strPrint += string.Format("Ref No  : {0}", dr["RefBillNo"]) + Environment.NewLine;
            strPrint += string.Format("C/N Remarks: {0}", dr["Remarks"]) + Environment.NewLine;
            strPrint += string.Format("Vehicle Type : {0} {1}", dr["VType"], string.IsNullOrEmpty(dr["PlateNo"].ToString()) ? string.Empty : "(" + dr["PlateNo"] + ")") + Environment.NewLine;
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
            if (GlobalClass.NoRawPrinter)
                new StringPrint(strPrint).Print();
            else
                RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Receipt");
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
