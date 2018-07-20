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
    class CreditNoteVoucherWiseViewModel : BaseViewModel
    {
        enum Focusable
        {
            Barcode = 0, CashAmount = 1
        }
        DateConverter nepDate;
        bool _PartyEnabled;
        DispatcherTimer timer;
        string _CurTime;
        DateTime _CurDate;
        private string _InvoicePrefix = "TI";
        private string _InvoiceNo;
        private bool _MustIssueTaxInvoice;
        private string _RefBillNo;
        private string _Remarks;
        private ObservableCollection<TParkingSalesDetails> _VSDetailList;
        private TParkingSales _VSales;

        public TParkingSales VSales { get { return _VSales; } set { _VSales = value; OnPropertyChanged("VSales"); } }
        public ObservableCollection<TParkingSalesDetails> VSDetailList { get { return _VSDetailList; } set { _VSDetailList = value; OnPropertyChanged("VSDetailList"); } }
       
        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public string RefBillNo { get { return _RefBillNo; } set { _RefBillNo = value; OnPropertyChanged("RefBillNo"); } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; OnPropertyChanged("Remarks"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public bool PartyEnabled { get { return _PartyEnabled; } set { _PartyEnabled = value; OnPropertyChanged("PartyEnabled"); } }
                
        public RelayCommand LoadInvoice { get { return new RelayCommand(ExecuteLoadInvoice, CanLoadInvoice); } }

        private void ExecuteLoadInvoice(object obj)
        {
            string BillNo = InvoicePrefix + obj.ToString();
            int PID;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if(conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingSales WHERE RefBillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }) > 0)
                    {
                        MessageBox.Show("Credit Note has already been issued to selected Bill.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                    if (PID > 0)
                        return;

                    VSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, UID, SESSION_ID, PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    VSDetailList = new ObservableCollection<TParkingSalesDetails>(
                                        conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
                    
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
        public CreditNoteVoucherWiseViewModel()
        {
            try
            {
                MessageBoxCaption = "Credit Note";                
                nepDate = new DateConverter(GlobalClass.TConnectionString);
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
                VSDetailList = new ObservableCollection<TParkingSalesDetails>();
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
                    VSales = new TParkingSales();
                    VSDetailList = new ObservableCollection<TParkingSalesDetails>();
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
                    PrintBill(CNO);
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
                    var CNote = conn.Query<TParkingSales>("SELECT * FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = CreditNoteNo, FYID = GlobalClass.FYID });
                    if (CNote.Count() > 0)
                    {
                        string BillNo = CNote.First().RefBillNo;
                        Remarks = CNote.First().Remarks;
                        
                        RefBillNo = BillNo.Substring(2, BillNo.Length - 2);
                        PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                        if (PID > 0)
                            return;
                        VSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, UID, SESSION_ID, PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                        VSDetailList = new ObservableCollection<TParkingSalesDetails>(
                                            conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
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
                        if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingSales WHERE RefBillNo = @RefBillNo AND FYID = @FYID", new { RefBillNo = InvoicePrefix + RefBillNo, FYID = GlobalClass.FYID }, tran) > 0)
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
                        
                        PrintBill(BillNo.ToString());
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
            VSales = new TParkingSales();
            VSDetailList = new ObservableCollection<TParkingSalesDetails>();
            SetAction(ButtonAction.Init);
            OnPropertyChanged("IsEntryMode");
        }

       




        void PrintBill(string BillNo, bool IsNew = false)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string PType = conn.ExecuteScalar<string>("SELECT PType From ParkingSales PS JOIN ParkingSalesDetails PSD ON PS.BillNo = PSD.BillNo AND PS.FYID = PSD.FYID WHERE PS.BillNo = @BillNo AND PS.FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                    if (string.IsNullOrEmpty(PType))
                    {
                        MessageBox.Show("Invalid Invoice No", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    else if (PType == "P")
                    {
                        MessageBox.Show("Parking Invoice cannot be loaded in this interface.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    var vSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, UserName [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, PS.UID, SESSION_ID, PID FROM ParkingSales PS JOIN Users U ON PS.UID = U.UID WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    var vSDetailList = new List<TParkingSalesDetails>(
                                        conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
                    string InWords = "Rs. " + conn.ExecuteScalar<string>("SELECT DBO.Num_ToWordsArabic(" + vSales.GrossAmount + ")");
                    string DuplicateCaption = (IsNew) ? String.Empty : GlobalClass.GetReprintCaption(BillNo);
                    var pslip = new CreditNote
                    {
                        CompanyName = GlobalClass.CompanyName,
                        CompanyAddress = GlobalClass.CompanyAddress,
                        CompanyPan = GlobalClass.CompanyPan,
                        PSales = vSales,
                        PSDetails = vSDetailList,
                        InWords = InWords,
                        DuplicateCaption = DuplicateCaption,
                        InvoiceTitle = "CREDIT NOTE"
                    };
                    pslip.Print();                   
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(Ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
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
