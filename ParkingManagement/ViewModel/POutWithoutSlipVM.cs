using DateFunction;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Dapper;
using System.Windows;
using System.Data;
using RawPrintFunctions;
using System.Collections.ObjectModel;
namespace ParkingManagement.ViewModel
{
    public class POutWithoutSlipVM : BaseViewModel
    {
        enum Focusable
        {
            Barcode = 0, CashAmount = 1
        }
        DateConverter nepDate;
        DispatcherTimer timer;
        ParkingIn _PIN;
        ParkingOut _POUT;
        string _CurTime;
        DateTime _CurDate;
        private string _InvoicePrefix;
        private string _InvoiceNo;
        private bool _TaxInvoice;
        private bool _MustIssueTaxInvoice;
        private ObservableCollection<VehicleType> _VTypeList;
        private DateTime _InDate;
        private DateTime _InTime;
        public ParkingIn PIN { get { return _PIN; } set { _PIN = value; OnPropertyChanged("PIN"); } }
        public ParkingOut POUT { get { return _POUT; } set { _POUT = value; OnPropertyChanged("POUT"); } }
        public bool TaxInvoice
        {
            get { return _TaxInvoice; }
            set
            {
                _TaxInvoice = value;
                OnPropertyChanged("TaxInvoice");
                InvoicePrefix = (value) ? "TI" : "SI";
                if (_action == ButtonAction.New)
                    InvoiceNo = GetInvoiceNo(InvoicePrefix);
            }
        }
        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public ObservableCollection<VehicleType> VTypeList { get { return _VTypeList; } set { _VTypeList = value; OnPropertyChanged("VTypeList"); } }
        public DateTime InDate { get { return _InDate; } set { _InDate = value; OnPropertyChanged("InDate"); } }
        public DateTime InTime { get { return _InTime; } set { _InTime = value; OnPropertyChanged("InTime"); } }

        public RelayCommand PrintCommand { get; set; }
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
            }
            catch (Exception)
            {

            }
        }

        private bool CanLoadInvoice(object obj)
        {
            return true;
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

        public POutWithoutSlipVM()
        {
            try
            {
                MessageBoxCaption = "Exit Without Slip";
                TaxInvoice = false;
                CanChangeInvoiceType = true;
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                PIN = new ParkingIn();
                InDate = DateTime.Today.Date;
                InTime = DateTime.Now;
                POUT = new ParkingOut();

                CurDate = DateTime.Today;
                CurTime = DateTime.Now.ToString("hh:mm tt");
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;
                timer.Start();
                LoadData = new RelayCommand(ExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                SetAction(ButtonAction.Init);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    VTypeList = new ObservableCollection<VehicleType>(conn.Query<VehicleType>("SELECT VTypeID, Description FROM VehicleType"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            InvoicePrefix = (TaxInvoice) ? "TI" : "SI";
            InvoiceNo = GetInvoiceNo(InvoicePrefix);
            SetAction(ButtonAction.New);
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
                    string BillNo = InvoicePrefix + InvoiceNo;
                    string DuplicateCaption = GlobalClass.GetReprintCaption(BillNo);
                    POutVMTouch.PrintBill(BillNo, conn, (TaxInvoice) ? "INVOICE" : "ABBREVIATED TAX INVOCE", DuplicateCaption);
                    GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                    GlobalClass.SetUserActivityLog("Exit", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                    
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
            return _action == ButtonAction.New;
        }
        private void ExecuteLoad(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    PIN.InDate = InDate;
                    PIN.InTime = InTime.ToString("hh:mm tt");
                    PIN.InMiti = nepDate.CBSDate(PIN.InDate);
                    PIN.VType = VTypeList.First(x => x.VTypeID == PIN.VehicleType);

                    POUT.Rate_ID = (int)conn.ExecuteScalar("SELECT RATE_ID FROM RATEMASTER WHERE IsDefault = 1");
                    POUT.OutDate = _CurDate;
                    POUT.OutTime = _CurTime;
                    POUT.OutMiti = nepDate.CBSDate(POUT.OutDate);
                    POUT.Interval = GetInterval(PIN.InDate, POUT.OutDate, PIN.InTime, POUT.OutTime);
                    POUT.PID = PIN.PID;

                    CalculateParkingCharge(conn, PIN.InDate.Add(DateTime.Parse(PIN.InTime).TimeOfDay), POUT.OutDate.Add(DateTime.Parse(POUT.OutTime).TimeOfDay), POUT.Rate_ID, PIN.VehicleType);
                    POUT.CashAmount = POUT.ChargedAmount;
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
                    PoleDisplay.WriteToDisplay(POUT.ChargedAmount, PoleDisplayType.AMOUNT);
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

        string GetInvoiceNo(string VNAME)
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

        string GetInvoiceNo(string VNAME, SqlTransaction tran)
        {
            string invoice = tran.Connection.ExecuteScalar<string>("SELECT CurNo FROM tblSequence WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
            if (string.IsNullOrEmpty(invoice))
            {
                tran.Connection.Execute("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
                invoice = "1";
            }
            return invoice;
        }

        private void ExecuteSave(object obj)
        {
            string strSQL;
            decimal Taxable, VAT, Amount, Discount, NonTaxable, Rate, Quantity;
            string BillNo = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        //PIN.PID = conn.ExecuteScalar<int>("SELECT CurNo FROM tblSequence WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                        PIN.PID = Convert.ToInt32(GetInvoiceNo("PID", tran));
                        PIN.Barcode = string.Empty;
                        PIN.Save(tran);
                        POUT.PID = PIN.PID;
                        POUT.Save(tran);
                        if (POUT.CashAmount > 0)
                        {
                            BillNo = InvoicePrefix + GetInvoiceNo(InvoicePrefix, tran);
                            Quantity = POUT.ChargedHours;
                            Amount = POUT.CashAmount / (1 + (GlobalClass.VAT / 100));
                            Rate = Amount / Quantity;
                            Discount = 0;
                            NonTaxable = 0;
                            Taxable = Amount - (NonTaxable + Discount);
                            VAT = Taxable * GlobalClass.VAT / 100;
                            TParkingSales PSales = new TParkingSales
                            {
                                BillNo = BillNo,
                                TDate = POUT.OutDate,
                                TMiti = POUT.OutMiti,
                                TTime = POUT.OutTime,
                                BillTo = POUT.BILLTO,
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
                            };
                            PSales.Save(tran);
                            TParkingSalesDetails PSalesDetails = new TParkingSalesDetails
                            {
                                BillNo = BillNo,
                                FYID = GlobalClass.FYID,
                                Quantity = Quantity,
                                Rate = Rate,
                                Amount = Amount,
                                Discount = Discount,
                                NonTaxable = NonTaxable,
                                Taxable = Taxable,
                                VAT = VAT,
                                NetAmount = POUT.CashAmount,
                                ProdId = 0, 
                                Description = "Parking Charge",
                                PType = 'P'
                            };
                            PSalesDetails.Save(tran);

                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                            GlobalClass.SetUserActivityLog("Exit", "New", VCRHNO: BillNo, WorkDetail: "PID : " + PIN.PID);
                            SyncFunctions.LogSyncStatus(tran, BillNo, GlobalClass.FYNAME);
                        }
                        tran.Commit();
                        if (!string.IsNullOrEmpty(SyncFunctions.username))
                        {
                            SyncFunctions.SyncSalesData(SyncFunctions.getBillObject(BillNo), 1);
                        }
                    }
                    if (!string.IsNullOrEmpty(BillNo))
                    {
                        RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)27).ToString() + ((char)112).ToString() + ((char)0).ToString() + ((char)64).ToString() + ((char)240).ToString(), "Receipt");   //Open Cash Drawer
                        POutVMTouch.PrintBill(BillNo.ToString(), conn, (TaxInvoice) ? "TAX INVOICE" : "ABBREVIATED TAX INVOCE");
                        if (TaxInvoice)
                        {
                            POutVMTouch.PrintBill(BillNo.ToString(), conn, "INVOICE");
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
            FocusedElement = (short)Focusable.Barcode;
            TaxInvoice = false;
            InvoiceNo = string.Empty;
            CanChangeInvoiceType = true;
            PIN = new ParkingIn();
            InDate = DateTime.Today.Date;
            InTime = DateTime.Now;
            POUT = new ParkingOut();
            SetAction(ButtonAction.Init);
            OnPropertyChanged("IsEntryMode");
            PoleDisplay.WriteToDisplay(POUT.ChargedAmount, PoleDisplayType.AMOUNT);
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
