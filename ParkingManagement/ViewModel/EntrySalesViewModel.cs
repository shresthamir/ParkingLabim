using Dapper;
using ParkingManagement.Forms;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using RawPrintFunctions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ParkingManagement.ViewModel
{
    public class EntrySalesViewModel : VoucherSalesInvoiceVM
    {
        public enum Focusable
        {
            Invoice = 0,
            VoucherType = 1,
            Customer = 2
        }
        public DateConverter nepDate;
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
        public List<Voucher> ParkingVouchers;
        private decimal _TotQty;
        string _GenCount;
        decimal _Progress;
        public wVoucherPrintProgress vp;
        private Party _SelectedCustomer;
        private ObservableCollection<Party> _CustomerList;

        public bool GenerateVoucher { get; set; }
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
        public ObservableCollection<Party> CustomerList { get { return _CustomerList; } set { _CustomerList = value; OnPropertyChanged("CustomerList"); } }
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
                    VSDetail.Rate = value.Rate;
                    //VSDetail.RateStr = VSDetail.Rate.ToString("#0.00");
                }
                _SelectedVoucherType = value;
                OnPropertyChanged("SelectedVoucherType");
            }
        }
        public Party SelectedCustomer
        {
            get { return _SelectedCustomer; }
            set
            {
                _SelectedCustomer = value;
                VSales.BillTo = value?.Name;
                VSales.BILLTOADD = value?.Address;
                VSales.BILLTOPAN = value?.PAN;
                OnPropertyChanged("SelectedCustomer");
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
            GenerateVoucher = !SelectedVoucherType.SkipVoucherGeneration;
            var vsDetail = new TParkingSalesDetails
            {
                FYID = GlobalClass.FYID,
                PType = 'V',
                ProdId = VSDetail.ProdId,
                Description = VSDetail.Description,
                Quantity = VSDetail.Quantity,
                QuantityStr = VSDetail.QuantityStr,
                Rate = VSDetail.Rate,
                RateStr = VSDetail.RateStr,
                Amount = VSDetail.Amount,
                Taxable = SelectedVoucherType.NonVat ? 0 : VSDetail.Amount,
                NonTaxable = SelectedVoucherType.NonVat ? VSDetail.Amount : 0,
                Remarks = VSDetail.Remarks
            };
            vsDetail.VAT = vsDetail.Taxable * GlobalClass.VAT / 100;
            vsDetail.NetAmount = vsDetail.Amount + vsDetail.VAT;
            VSDetailList.Add(vsDetail);
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
                        MessageBox.Show("Entrance Invoice cannot be loaded in this interface.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

        public EntrySalesViewModel()
        {
            try
            {
                MessageBoxCaption = "Entry Sales Invoice";
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
                    VTypeList = new ObservableCollection<VoucherType>(conn.Query<VoucherType>("SELECT VoucherId, VoucherName, Rate, Value, ValidStart, ValidEnd, Validity, VoucherInfo, SkipVoucherGeneration, ISNULL(NonVat,0 ) NonVat FROM VoucherTypes"));
                    CustomerList = new ObservableCollection<Party>(conn.Query<Party>("SELECT DISTINCT BillTo Name, BillToAdd Address, BillToPan PAN FROM ParkingSales where BillTo IS NOT NULL"));
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
                VSales.Taxable = VSDetailList.Sum(x => x.Taxable);
                VSales.NonTaxable = VSDetailList.Sum(x => x.NonTaxable);
                TotQty = VSDetailList.Sum(x => x.Quantity);
            }
        }

        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            InvoiceNo = GetInvoiceNo(InvoicePrefix);
            SetAction(ButtonAction.New);
            FocusedElement = (short)Focusable.Customer;
            VSDetail.PropertyChanged += VSDetail_PropertyChanged;

        }


        private bool CanExecutePrint(object obj)
        {
            return _action == ButtonAction.InvoiceLoaded;
        }

        private async void ExecutePrint(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    //PrintBill(VSales.BillNo);
                    Print(VSales.BillNo, "INVOICE");
                    string DuplicateCaption = GlobalClass.GetReprintCaption(VSales.BillNo);
                    GlobalClass.SavePrintLog(VSales.BillNo, null, DuplicateCaption);
                    GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "Re-Print", WorkDetail: string.Empty, VCRHNO: VSales.BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                    if (MessageBox.Show("Would you like to reprint Vouchers as well?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        VoucherSelection vs = conn.Query<VoucherSelection>("SELECT MIN(VoucherNo) VNOFrom, MAX(VoucherNo) VNOTo FROM ParkingVouchers WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = VSales.BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                        wVoucherSelect wVS = new wVoucherSelect() { DataContext = vs };
                        wVS.ShowDialog();

                        vp = new wVoucherPrintProgress() { DataContext = this };
                        vp.Show();
                        await PrintVouchers(VSales.BillNo, VSales.Amount, VSDetailList[0].Description, true);
                        vp.Close();
                    }
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

        public string GetInvoiceNo(string VNAME)
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
                if (!VSales.TRNMODE && string.IsNullOrEmpty(VSales.BillTo))
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
                        VSales.ExpiryDate = CurDate;
                        VSales.Save(tran);
                        foreach (TParkingSalesDetails PSD in VSDetailList)
                        {
                            PSD.BillNo = VSales.BillNo;
                            PSD.FYID = VSales.FYID;
                            PSD.Save(tran);
                        }

                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                        GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "New", VCRHNO: VSales.BillNo, WorkDetail: "Bill No : " + VSales.BillNo);

                        SyncFunctions.LogSyncStatus(tran, VSales.BillNo, GlobalClass.FYNAME);
                        if (GenerateVoucher)
                        {
                            vp = new wVoucherPrintProgress() { DataContext = this };
                            vp.Show();
                            await GenerateVouchers(tran);
                            vp.Hide();
                        }
                        tran.Commit();
                        if (!string.IsNullOrEmpty(SyncFunctions.username))
                        {
                            SyncFunctions.SyncSalesData(SyncFunctions.getBillObject(VSales.BillNo), 1);
                        }
                        MessageBox.Show("Vouchers Generated Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        if (!(string.IsNullOrEmpty(VSales.BillTo) || CustomerList.Any(x => x.Name == VSales.BillTo)))
                            CustomerList.Add(new Party { Name = VSales.BillTo, Address = VSales.BILLTOADD, PAN = VSales.BILLTOPAN });
                    }
                    if (!string.IsNullOrEmpty(VSales.BillNo))
                    {
                        //PrintBill(VSales.BillNo, true);
                        Print(VSales.BillNo, "ABBREVIATED TAX INVOICE");

                        if (GenerateVoucher)
                        {
                            vp.Show();
                            await PrintVouchers(VSales.BillNo, VSales.Amount, VSDetailList[0].Description, true);
                            vp.Close();
                        }
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

        //private void GenerateVouchers()
        //{
        //    decimal Total = VSDetailList.Sum(x => x.Quantity);
        //    string Status = "{0} of " + Total.ToString() + " Done";
        //    Dispatcher d = Dispatcher.CurrentDispatcher;
        //    ThreadPool.QueueUserWorkItem(x =>
        //    {
        //        using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
        //        {
        //            conn.Open();
        //            using (SqlTransaction tran = conn.BeginTransaction())
        //            {
        //                foreach (TParkingSalesDetails pSD in _VSDetailList)
        //                {
        //                    VoucherType vt = VTypeList.First(y => y.VoucherId == pSD.ProdId && y.SkipVoucherGeneration == false);
        //                    for (int i = 1; i <= pSD.Quantity; i++)
        //                    {
        //                        Voucher v = new Voucher()
        //                        {
        //                            BillNo = pSD.BillNo,
        //                            ExpDate = CurDate.AddDays(vt.Validity),
        //                            ValidStart = vt.ValidStart,
        //                            ValidEnd = vt.ValidEnd,
        //                            VoucherName = vt.VoucherName,
        //                            Value = vt.Value,
        //                            Sno = i,
        //                            VoucherId = vt.VoucherId,
        //                            FYID = GlobalClass.FYID
        //                        };
        //                        do
        //                        {
        //                            v.Barcode = "#" + new Random().Next(1677215).ToString("X");
        //                        }
        //                        while (tran.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingVouchers WHERE Barcode = @Barcode", v, transaction: tran) > 0);
        //                        v.Save(tran);
        //                        ParkingVouchers.Add(v);
        //                        d.BeginInvoke((Action)(() =>
        //                        {
        //                            GenCount = string.Format(Status, ParkingVouchers.Count);
        //                            Progress = ParkingVouchers.Count / Total * 100;
        //                        }));
        //                    }
        //                }
        //                tran.Commit();
        //                PrintVouchers(_VSDetailList.First().BillNo, true, d);
        //            }
        //        }
        //    });
        //}

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

        public async Task PrintVouchers(string BillNo, decimal amount, string Description, bool IsNew, VoucherSelection vs = null)
        {
            string Condition = string.Empty;
            Progress = 0;
            int counter = 1;
            List<Voucher> Barcodes = new List<Voucher>();
            if (vs != null)
            {
                Condition = string.Format(" AND VoucherNo BETWEEN {0} AND {1}", vs.VNOFrom, vs.VNOTo);
            }
            //await Task.Run(() =>
            //{
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                var Vouchers = conn.Query<Voucher>("SELECT VoucherNo, VoucherInfo VoucherName, PV.VoucherId, Barcode  FROM ParkingVouchers PV JOIN VoucherTypes VT ON PV.VoucherId = VT.VoucherId WHERE BillNo = @BillNo AND FYID = @FYID" + Condition, new { BillNo = BillNo, FYID = GlobalClass.FYID });
                decimal Total = Vouchers.Count();
                string Status = "{0} of " + Total.ToString() + " Printed";

                var vSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, UserName [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, PS.UID, SESSION_ID, PID FROM ParkingSales PS JOIN Users U ON PS.UID = U.UID WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();

                foreach (Voucher v in Vouchers)
                {
                    //if (Barcodes.Count <= 3)
                    Barcodes.Add(v);
                    //else
                    //{
                    //    new EntranceTicketPrint(Barcodes).Print();
                    //    GenCount = string.Format(Status, counter);
                    //    Progress = counter / Total * 100;
                    //    Thread.Sleep(3000);
                    //    Barcodes.Clear();
                    //    Barcodes.Add(v);
                    //}
                    counter++;
                }
                if (Barcodes.Count > 0)
                {
                    foreach (var barcode in Barcodes)
                    {
                        new EntranceTicketPrint(barcode, vSales, amount, Description).Print();
                        GenCount = string.Format(Status, Total);
                        Progress = 100;
                    }

                }
            }
            //});
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

        public void ExecuteUndo(object obj)
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
                        MessageBox.Show("Entrance Invoice cannot be loaded in this interface.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    var vSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, UserName [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, PS.UID, SESSION_ID, PID FROM ParkingSales PS JOIN Users U ON PS.UID = U.UID WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    var vSDetailList = new List<TParkingSalesDetails>(
                                        conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
                    string InWords = GlobalClass.GetNumToWords(conn, vSales.GrossAmount);

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
        void Print(string BillNo, string Invoice, string DuplicateCaption = "")
        {
            try
            {
                //string Invoice = "ABBREVIATED TAX INVOICE";
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
                        MessageBox.Show("Entrance Invoice cannot be loaded in this interface.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    var vSales = conn.Query<TParkingSales>("SELECT BillNo, FYID, TDate, TMiti, TTime, UserName [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, PS.UID, SESSION_ID, PID FROM ParkingSales PS JOIN Users U ON PS.UID = U.UID WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    var vSDetailList = new List<TParkingSalesDetails>(
                                        conn.Query<TParkingSalesDetails>("SELECT BillNo, FYID, PTYPE, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks FROM ParkingSalesDetails WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }));
                    string InWords = GlobalClass.GetNumToWords(conn, vSales.GrossAmount);

                    //string DuplicateCaption = (IsNew) ? String.Empty : GlobalClass.GetReprintCaption(BillNo);


                    string strPrint = string.Empty;
                    int PrintLen = 40;
                    string PAN = "PAN : " + GlobalClass.CompanyPan;
                    string Particulars = "Particulars";
                    //Particulars = (Particulars.Length > PrintLen - 17) ? Particulars.Substring(0, PrintLen - 17) : Particulars.PadLeft((PrintLen + Particulars.Length - 17) / 2, ' ').PadRight(PrintLen - 17, ' ');



                    strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
                    strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
                    strPrint += PAN.PadLeft((PrintLen + PAN.Length) / 2, ' ') + Environment.NewLine;
                    strPrint += Invoice.PadLeft((PrintLen + Invoice.Length) / 2, ' ') + Environment.NewLine;
                    if (!string.IsNullOrEmpty(DuplicateCaption))
                    {
                        strPrint += DuplicateCaption.PadLeft((PrintLen + DuplicateCaption.Length) / 2, ' ') + Environment.NewLine;
                    }

                    strPrint += string.Format("Bill No : {0}    Date : {1}", BillNo.PadRight(7, ' '), vSales.TMiti) + Environment.NewLine;
                    //strPrint += string.Format("Vehicle Type : {0} {1}", dr["VType"], string.IsNullOrEmpty(dr["PlateNo"].ToString()) ? string.Empty : "(" + dr["PlateNo"] + ")") + Environment.NewLine;
                    strPrint += string.Format("Name    : {0}", vSales.BillTo) + Environment.NewLine;
                    strPrint += string.Format("Address : {0}", vSales.BILLTOADD) + Environment.NewLine;
                    strPrint += string.Format("PAN     : {0}", vSales.BILLTOPAN) + Environment.NewLine;
                    strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
                    strPrint += string.Format("Sn.|{0}| Qty | Rate | Amount  |", Particulars) + Environment.NewLine;
                    int count = 1;
                    foreach (var item in vSDetailList)
                    {
                        string Description = item.Description;
                        //Description = (Description.Length > PrintLen - 17) ? Description.Substring(0, PrintLen - 17) : Description.PadRight(PrintLen - 17, ' ');

                        strPrint += string.Format("{0}  {1}  {2}   {3}   {4}", count, Description, item.Quantity.ToString("#0.00"), item.Rate.ToString("#0.00"), item.Amount.ToString("#,##,##0.00")) + Environment.NewLine;
                        count++;
                    }
                    //strPrint += string.Format("    IN  : {0} {1}", dr["InTime"], dr["InMiti"]) + Environment.NewLine;
                    //strPrint += string.Format("    OUT : {0} {1}", dr["OutTime"], dr["OutMiti"]) + Environment.NewLine;
                    //strPrint += string.Format("    Interval : {0} ", dr["Interval"]) + Environment.NewLine;
                    //strPrint += string.Format("    Charged Hours : {0} ", dr["ChargedHours"]) + Environment.NewLine;

                    strPrint += Environment.NewLine;
                    strPrint += "---------------------------".PadLeft(PrintLen, ' ') + Environment.NewLine;

                    //strPrint += ("Net Amount : " + GParse.ToDecimal(vSales.Amount).ToString("#0.00").PadLeft(12, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                    //if (GParse.ToDecimal(vSales.Discount) > 0)
                    //{
                    //    strPrint += ("Discount : " + GParse.ToDecimal(vSales.Discount).ToString("#0.00").PadLeft(12, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                    //}

                    //strPrint += ("Taxable : " + GParse.ToDecimal(vSales.Taxable).ToString("#0.00").PadLeft(12, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                    //strPrint += ("Non Taxable : " + GParse.ToDecimal(vSales.NonTaxable).ToString("#0.00").PadLeft(12, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                    //strPrint += ("VAT 13% : " + GParse.ToDecimal(vSales.VAT).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                    strPrint += ("Amount : " + GParse.ToDecimal(vSales.GrossAmount).ToString("#0.00").PadLeft(12, ' ')).PadLeft(PrintLen, ' ');

                    strPrint += Environment.NewLine;
                    strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
                    strPrint += InWords + Environment.NewLine;
                    strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
                    strPrint += string.Format("Cashier : {0} ({1})", GlobalClass.User.UserName, DateTime.Now.ToString("HH:mm:ss tt")) + Environment.NewLine;
                    strPrint += Environment.NewLine;
                    strPrint += Environment.NewLine;
                    strPrint += Environment.NewLine;
                    strPrint += Environment.NewLine;
                    strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
                    strPrint += ((char)29).ToString() + ((char)86).ToString() + ((char)1).ToString();

                    //if (GlobalClass.NoRawPrinter)
                    //{
                    //    new StringPrint(strPrint).Print();
                    //}
                    //else
                    {
                        RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Receipt");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
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
