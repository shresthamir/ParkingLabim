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
using System.Threading;
using ParkingManagement.Forms;

namespace ParkingManagement.ViewModel
{
    class VoucherSalesInvoiceVM : BaseViewModel
    {
        enum Focusable
        {
            Invoice = 0,
            VoucherType = 1,
            Customer = 2
        }
        DateConverter nepDate;
        DispatcherTimer timer;
        string _CurTime;
        DateTime _CurDate;
        private string _InvoicePrefix = "TI";
        private string _InvoiceNo;
        private bool _TaxInvoice;
        private VoucherType _SelectedVoucherType;
        private ObservableCollection<VoucherType> _VTypeList;
        private TParkingSales _VSales;
        private TParkingSalesDetails _VSDetail;
        private TParkingSalesDetails _SelectedVSDetail;
        private ObservableCollection<TParkingSalesDetails> _VSDetailList;
        private List<Voucher> ParkingVouchers;
        private decimal _TotQty;
        string _GenCount;
        decimal _Progress;
        wVoucherPrintProgress vp;

        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public ObservableCollection<VoucherType> VTypeList { get { return _VTypeList; } set { _VTypeList = value; OnPropertyChanged("VTypeList"); } }
        public TParkingSales VSales { get { return _VSales; } set { _VSales = value; OnPropertyChanged("VSales"); } }
        public TParkingSalesDetails VSDetail { get { return _VSDetail; } set { _VSDetail = value; OnPropertyChanged("VSDetail"); } }
        public TParkingSalesDetails SelectedVSDetail { get { return _SelectedVSDetail; } set { _SelectedVSDetail = value; OnPropertyChanged("SelectedVSDetail"); } }
        public ObservableCollection<TParkingSalesDetails> VSDetailList { get { return _VSDetailList; } set { _VSDetailList = value; OnPropertyChanged("VSDetailList"); } }
        public decimal TotQty { get { return _TotQty; } set { _TotQty = value; OnPropertyChanged("TotQty"); } }
        public string GenCount { get { return _GenCount; } set { _GenCount = value; OnPropertyChanged("GenCount"); } }
        public decimal Progress { get { return _Progress; } set { _Progress = value; OnPropertyChanged("Progress"); } }

        public VoucherType SelectedVoucherType
        {
            get { return _SelectedVoucherType; }
            set
            {
                if (_SelectedVoucherType != value && value != null)
                {
                    VSDetail.Description = value.VoucherName;
                    VSDetail.Rate = value.Rate / (1 + (GlobalClass.VAT / 100));
                }
                _SelectedVoucherType = value;
                OnPropertyChanged("SelectedVoucherType");
            }
        }

        public RelayCommand RePrintCommand { get { return new RelayCommand(ExecuteRePrint, CanExecuteRePrint); } }
        public RelayCommand LoadInvoice { get { return new RelayCommand(ExecuteLoadInvoice, CanLoadInvoice); } }
        public RelayCommand AddVoucherCommand { get { return new RelayCommand(AddVoucher, CanAddVoucher); } }


        private bool CanAddVoucher(object obj)
        {
            return true;
        }

        private void AddVoucher(object obj)
        {

            if (VSDetail.ProdId == 0)
            {
                MessageBox.Show("Please Select a Voucher first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (VSDetail.Quantity == 0)
            {
                MessageBox.Show("Please enter the quantity first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (VSDetailList.Any(x => x.ProdId == VSDetail.ProdId))
            {
                MessageBox.Show("Selected Voucher is already added.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            VSDetailList.Add(new TParkingSalesDetails
            {
                FYID = GlobalClass.FYID,
                PType = 'V',
                ProdId = VSDetail.ProdId,
                Description = VSDetail.Description,
                Quantity = VSDetail.Quantity,
                Rate = VSDetail.Rate,
                Amount = VSDetail.Amount,
                Taxable = VSDetail.Amount,
                VAT = VSDetail.Amount * GlobalClass.VAT / 100,
                NetAmount = VSDetail.Amount * (1 + GlobalClass.VAT / 100)
            });
            VSDetail = new TParkingSalesDetails();
            VSDetail.PropertyChanged += VSDetail_PropertyChanged;
            FocusedElement = (short)Focusable.VoucherType;
        }

        private void VSDetail_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Rate" || e.PropertyName == "Quantity")
                VSDetail.Amount = VSDetail.Rate * VSDetail.Quantity;
        }

        private void ExecuteLoadInvoice(object obj)
        {
            string BillNo = InvoicePrefix + obj.ToString();
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

                    VSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, UID, SESSION_ID, PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    VSDetailList = new ObservableCollection<TParkingSalesDetails>(
                                        conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
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

        public VoucherSalesInvoiceVM()
        {
            try
            {
                MessageBoxCaption = "Voucher Sales Invoice";
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                CurDate = DateTime.Today;
                CurTime = DateTime.Now.ToString("hh:mm tt");
                VSDetail = new TParkingSalesDetails();
                VSDetailList = new ObservableCollection<TParkingSalesDetails>();
                VSDetailList.CollectionChanged += VSDetailList_CollectionChanged;
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;
                timer.Start();
                NewCommand = new RelayCommand(ExecuteNew);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                SetAction(ButtonAction.Init);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    VTypeList = new ObservableCollection<VoucherType>(conn.Query<VoucherType>("SELECT VoucherId, VoucherName, Rate, Value, ValidStart, ValidEnd, Validity, VoucherInfo, SkipVoucherGeneration FROM VoucherTypes"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VSDetailList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (VSDetailList != null)
            {
                VSales.Amount = VSDetailList.Sum(x => x.Amount);
                VSales.VAT = VSDetailList.Sum(x => x.VAT);
                VSales.GrossAmount = VSDetailList.Sum(x => x.NetAmount);
                VSales.Taxable = VSDetailList.Sum(x => x.Amount);
                TotQty = VSDetailList.Sum(x => x.Quantity);
            }
        }

        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            InvoiceNo = GetInvoiceNo(InvoicePrefix);
            SetAction(ButtonAction.New);
            FocusedElement = (short)Focusable.Customer;
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
                    PrintBill(VSales.BillNo);
                    //string BillNo = InvoicePrefix + InvoiceNo;
                    //string DuplicateCaption = GlobalClass.GetReprintCaption(BillNo);
                    //POutVMTouch.PrintBill(BillNo, conn, "INVOICE", DuplicateCaption);
                    //GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                    //GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);

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

        private async void ExecuteSave(object obj)
        {
            try
            {
                if (VSales.TRNMODE && string.IsNullOrEmpty(VSales.BillTo))
                {
                    MessageBox.Show("Customer Name cannot be empty in Credit Sales.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else if (VSDetailList.Count == 0)
                {
                    MessageBox.Show("Please add atleast one Voucher Type.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (MessageBox.Show("You are going to save current transation. Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                ParkingVouchers = new List<Voucher>();
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        VSales.TDate = CurDate;
                        VSales.TTime = CurTime;
                        VSales.UID = GlobalClass.User.UID;
                        VSales.SESSION_ID = GlobalClass.Session;
                        VSales.TMiti = nepDate.CBSDate(CurDate);
                        VSales.BillNo = InvoicePrefix + GetInvoiceNo(InvoicePrefix);
                        VSales.FYID = GlobalClass.FYID;
                        VSales.Save(tran);
                        foreach (TParkingSalesDetails PSD in VSDetailList)
                        {
                            PSD.BillNo = VSales.BillNo;
                            PSD.FYID = VSales.FYID;
                            PSD.Save(tran);
                        }

                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                        GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "New", VCRHNO: VSales.BillNo, WorkDetail: "Bill No : " + VSales.BillNo);
                        vp = new wVoucherPrintProgress() { DataContext = this };
                        vp.Show();
                        await GenerateVouchers(tran);
                        vp.Hide();
                        tran.Commit();
                        MessageBox.Show("Vouchers Generated Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);                 
                    }
                    if (!string.IsNullOrEmpty(VSales.BillNo))
                    {
                        PrintBill(VSales.BillNo, true);
                        vp.Show();
                        await PrintVouchers(VSales.BillNo, true);
                        vp.Close();
                    }
                    //GenerateVouchers();

                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateVouchers()
        {
            decimal Total = VSDetailList.Sum(x => x.Quantity);
            string Status = "{0} of " + Total.ToString() + " Done";
            Dispatcher d = Dispatcher.CurrentDispatcher;
            ThreadPool.QueueUserWorkItem(x =>
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        foreach (TParkingSalesDetails pSD in _VSDetailList)
                        {
                            VoucherType vt = VTypeList.First(y => y.VoucherId == pSD.ProdId && y.SkipVoucherGeneration == false);
                            for (int i = 1; i <= pSD.Quantity; i++)
                            {
                                Voucher v = new Voucher()
                                {
                                    BillNo = pSD.BillNo,
                                    ExpDate = CurDate.AddDays(vt.Validity),
                                    ValidStart = vt.ValidStart,
                                    ValidEnd = vt.ValidEnd,
                                    VoucherName = vt.VoucherName,
                                    Value = vt.Value,
                                    Sno = i,
                                    VoucherId = vt.VoucherId,
                                    FYID = GlobalClass.FYID
                                };
                                do
                                {
                                    v.Barcode = "#" + new Random().Next(1677215).ToString("X");
                                }
                                while (tran.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingVouchers WHERE Barcode = @Barcode", v, transaction: tran) > 0);
                                v.Save(tran);
                                ParkingVouchers.Add(v);
                                d.BeginInvoke((Action)(() =>
                                {
                                    GenCount = string.Format(Status, ParkingVouchers.Count);
                                    Progress = ParkingVouchers.Count / Total * 100;
                                }));
                            }
                        }
                        tran.Commit();
                        PrintVouchers(_VSDetailList.First().BillNo, true, d);
                    }
                }
            });
        }

        private async Task GenerateVouchers(SqlTransaction tran)
        {
            decimal Total = VSDetailList.Sum(x => x.Quantity);
            string Status = "{0} of " + Total.ToString() + " Done";
            await Task.Run(() =>
            {
                foreach (TParkingSalesDetails pSD in _VSDetailList)
                {
                    VoucherType vt = VTypeList.FirstOrDefault(y => y.VoucherId == pSD.ProdId && y.SkipVoucherGeneration == false);
                    if (vt != null)
                    {
                        for (int i = 1; i <= pSD.Quantity; i++)
                        {
                            Voucher v = new Voucher()
                            {
                                BillNo = pSD.BillNo,
                                ExpDate = CurDate.AddDays(vt.Validity),
                                ValidStart = vt.ValidStart,
                                ValidEnd = vt.ValidEnd,
                                VoucherName = vt.VoucherName,
                                Value = vt.Value,
                                Sno = i,
                                VoucherId = vt.VoucherId,
                                FYID = GlobalClass.FYID
                            };
                            do
                            {
                                v.Barcode = "#" + new Random().Next(1677215).ToString("X");
                            }
                            while (tran.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingVouchers WHERE Barcode = @Barcode", v, transaction: tran) > 0);
                            v.Save(tran);
                            ParkingVouchers.Add(v);
                            GenCount = string.Format(Status, ParkingVouchers.Count);
                            Progress = ParkingVouchers.Count / Total * 100;
                        }
                    }
                }
            });
        }

        private async Task PrintVouchers(string BillNo, bool IsNew, VoucherSelection vs = null)
        {
            string Condition = string.Empty;
            Progress = 0;
            int counter = 1;
            List<Voucher> Barcodes = new List<Voucher>();
            if (vs != null)
            {
                Condition = string.Format(" AND VoucherNo BETWEEN {0} AND {1}", vs.VNOFrom, vs.VNOTo);
            }
            await Task.Run(() =>
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    var Vouchers = conn.Query<Voucher>("SELECT VoucherNo, VoucherInfo VoucherName, PV.VoucherId, Barcode  FROM ParkingVouchers PV JOIN VoucherTypes VT ON PV.VoucherId = VT.VoucherId WHERE BillNo = @BillNo AND FYID = @FYID" + Condition, new { BillNo = BillNo, FYID = GlobalClass.FYID });
                    decimal Total = Vouchers.Count();
                    string Status = "{0} of " + Total.ToString() + " Printed";
                    foreach (Voucher v in Vouchers)
                    {
                        if (Barcodes.Count <= 3)
                            Barcodes.Add(v);
                        else
                        {
                            new VoucherPrint(Barcodes.ToArray()).Print();
                            GenCount = string.Format(Status, counter);
                            Progress = counter / Total * 100;
                            Thread.Sleep(3000);
                            Barcodes.Clear();
                            Barcodes.Add(v);
                        }
                        counter++;
                    }
                    if (Barcodes.Count > 0)
                    {
                        new VoucherPrint(Barcodes.ToArray()).Print();
                        GenCount = string.Format(Status, Total);
                        Progress = 100;
                    }
                }
            });
        }

        private void PrintVouchers(string BillNo, bool IsNew, Dispatcher d, VoucherSelection vs = null)
        {
            string Condition = string.Empty;
            Progress = 0;
            int counter = 1;
            List<Voucher> Barcodes = new List<Voucher>();
            if (vs != null)
            {
                Condition = string.Format(" AND VoucherNo BETWEEN {0} AND {1}", vs.VNOFrom, vs.VNOTo);
            }
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                var Vouchers = conn.Query<Voucher>("SELECT VoucherNo, VoucherInfo VoucherName, PV.VoucherId, Barcode  FROM ParkingVouchers PV JOIN VoucherTypes VT ON PV.VoucherId = VT.VoucherId WHERE BillNo = @BillNo AND FYID = @FYID" + Condition, new { BillNo = BillNo, FYID = GlobalClass.FYID });
                decimal Total = Vouchers.Count();
                string Status = "{0} of " + Total.ToString() + " Printed";
                foreach (Voucher v in Vouchers)
                {
                    if (Barcodes.Count <= 3)
                        Barcodes.Add(v);
                    else
                    {
                        new VoucherPrint(Barcodes.ToArray()).Print();
                        d.BeginInvoke((Action)(() =>
                        {
                            GenCount = string.Format(Status, counter);
                            Progress = counter / Total * 100;
                        }));
                        Thread.Sleep(3000);
                        Barcodes.Clear();
                        Barcodes.Add(v);
                    }
                    counter++;
                }
                if (Barcodes.Count > 0)
                {
                    new VoucherPrint(Barcodes.ToArray()).Print();
                    d.BeginInvoke((Action)(() =>
                    {
                        GenCount = string.Format(Status, Total);
                        Progress = 100;
                        vp.Close();
                    }));
                }
            }
        }

        private void ExecuteUndo(object obj)
        {

            VSales = new TParkingSales();
            VSDetail = new TParkingSalesDetails();
            VSDetailList = new ObservableCollection<TParkingSalesDetails>();
            VSDetailList.CollectionChanged += VSDetailList_CollectionChanged;
            VSDetail.PropertyChanged += VSDetail_PropertyChanged;
            FocusedElement = (short)Focusable.Invoice;
            InvoiceNo = string.Empty;
            SetAction(ButtonAction.Init);
            OnPropertyChanged("IsEntryMode");
        }

        async void PrintBill(string BillNo, bool IsNew = false)
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
                    var pslip = new BillPrint
                    {
                        CompanyName = GlobalClass.CompanyName,
                        CompanyAddress = GlobalClass.CompanyAddress,
                        CompanyPan = GlobalClass.CompanyPan,
                        PSales = vSales,
                        PSDetails = vSDetailList,
                        InWords = InWords,
                        DuplicateCaption = DuplicateCaption
                    };
                    if (IsNew)
                    {
                        pslip.InvoiceTitle = "TAX INVOICE";
                        pslip.Print();
                        pslip.InvoiceTitle = "INVOIVE";
                        pslip.Print();
                    }
                    else
                    {
                        pslip.InvoiceTitle = "INVOIVE";
                        pslip.Print();
                        GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                        GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                        if (MessageBox.Show("Would you like to reprint Vouchers as well?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            VoucherSelection vs = conn.Query<VoucherSelection>("SELECT MIN(VoucherNo) VNOFrom, MAX(VoucherNo) VNOTo FROM ParkingVouchers WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                            wVoucherSelect wVS = new wVoucherSelect() { DataContext = vs };
                            wVS.ShowDialog();

                            vp = new wVoucherPrintProgress() { DataContext = this };
                            vp.Show();
                            await PrintVouchers(BillNo, false, vs);
                            vp.Close();


                        }
                    }
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
