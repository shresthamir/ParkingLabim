using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using ParkingManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParkingManagement.ViewModel
{
    public class CardSalesInvoiceViewModel : VoucherSalesInvoiceVM
    {

        private ObservableCollection<Member> _MemberList;
        private ObservableCollection<MembershipScheme> _SchemeList;
        private MembershipScheme _SelectedScheme;
        private Member _SelectedMember;
        private ObservableCollection<Device> _Device;
        private string _CardNumber;

        public ObservableCollection<Member> MemberList { get { return _MemberList; } set { _MemberList = value; OnPropertyChanged("MemberList"); } }
        public Member SelectedMember { get { return _SelectedMember; } set { _SelectedMember = value; OnPropertyChanged("SelectedMember"); } }
        public ObservableCollection<MembershipScheme> SchemeList { get { return _SchemeList; } set { _SchemeList = value; OnPropertyChanged("SchemeList"); } }
        public MembershipScheme SelectedScheme { get { return _SelectedScheme; } set { _SelectedScheme = value; OnPropertyChanged("SelectedScheme"); } }
        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }
        public string CardNumber { get { return _CardNumber; } set { _CardNumber = value; OnPropertyChanged("CardNumber"); } }



        public new RelayCommand AddVoucherCommand { get { return new RelayCommand(AddCard, CanAddCard); } }
        public RelayCommand CardNumberCommand { get { return new RelayCommand(AddCard, CanAddCard); } }


        private bool CanAddCard(object obj)
        {
            return true;
        }
        private void VSDetail_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProdId" && VSDetail.ProdId > 0)
            {
                VSales.ExpiryDate = VSales.TDate.AddDays(SchemeList.FirstOrDefault(x => x.SchemeId == VSDetail.ProdId).ValidityPeriod);
            }
        }
        private void AddCard(object obj)
        {
            if (string.IsNullOrWhiteSpace(CardNumber))
            {
                MessageBox.Show("Please enter card number.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (VSDetail.ProdId == 0)
            {
                SelectedMember = GetSchemeByCardNumber(CardNumber);
                if (SelectedMember == null)
                {
                    MessageBox.Show("Member with this Cardnumber not found..", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                VSDetail.ProdId = SelectedMember.SchemeId;
                //MessageBox.Show("Please Select Membership Scheme first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //return;
            }
            else if (VSDetailList.Any(x => x.ProdId == VSDetail.ProdId))
            {
                MessageBox.Show("Selected Card is already added.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (VSDetail.ProdId != 0)
            {
                SelectedMember.SchemeId = VSDetail.ProdId;

                VSales.ExpiryDate = VSales.TDate.AddDays(SchemeList.FirstOrDefault(x => x.SchemeId == VSDetail.ProdId).ValidityPeriod);
            }
            if (SelectedScheme is null)
            {
                SelectedScheme = SchemeList.FirstOrDefault(x => x.SchemeId == VSDetail.ProdId);
            }
            //GenerateVoucher = !SelectedVoucherType.SkipVoucherGeneration;
            var vsDetail = new TParkingSalesDetails
            {
                FYID = GlobalClass.FYID,
                PType = 'C',
                ProdId = VSDetail.ProdId,
                Description = SelectedScheme.SchemeName,
                Quantity = 1,
                //QuantityStr = VSDetail.QuantityStr,
                Rate = SelectedScheme.Rate,
                //RateStr = VSDetail.RateStr,
                Amount = SelectedScheme.Rate,
                //Taxable = SelectedVoucherType.NonVat ? 0 : VSDetail.Amount,
                Taxable = SelectedScheme.Rate,
                //NonTaxable = SelectedVoucherType.NonVat ? VSDetail.Amount : 0,
                //Remarks = VSDetail.Remarks
            };
            vsDetail.VAT = vsDetail.Taxable * GlobalClass.VAT / 100;
            vsDetail.NetAmount = vsDetail.Amount + vsDetail.VAT;
            VSDetailList.Clear();
            VSDetailList.Add(vsDetail);
            VSDetail = new TParkingSalesDetails();
            FocusedElement = (short)Focusable.VoucherType;
        }

        private Member GetSchemeByCardNumber(string cardNumber)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                string sql = "select m.*,s.SchemeName from members m join MembershipScheme s on m.SchemeId=s.SchemeId where BARCODE=@cardNumber";
                var result = conn.Query<Member>(sql, new { cardNumber }).FirstOrDefault();
                return result;
            }

        }

        public CardSalesInvoiceViewModel()
        {
            //SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            //UndoCommand = new RelayCommand(ExecuteUndo);
            //PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
            SetAction(ButtonAction.Init);
            NewCommand = new RelayCommand(ExecuteNew);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);

            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                MemberList = new ObservableCollection<Member>(conn.Query<Member>("select m.*,s.SchemeName from members m join MembershipScheme s on m.SchemeId=s.SchemeId"));
                SchemeList = new ObservableCollection<MembershipScheme>(conn.Query<MembershipScheme>("SELECT * FROM MembershipScheme"));
            }
            GetDeviceList();
        }

        private bool CanExecuteSave(object obj)
        {
            return true;
        }

        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            InvoiceNo = GetInvoiceNo(InvoicePrefix);
            SetAction(ButtonAction.New);
            FocusedElement = (short)Focusable.Customer;
            VSDetail.PropertyChanged += VSDetail_PropertyChanged;
        }
        private void ExecuteSave(object obj)
        {
            try
            {
                if (SelectedMember == null)
                {
                    MessageBox.Show("Please select member first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (string.IsNullOrWhiteSpace(SelectedMember.Barcode))
                {
                    MessageBox.Show("The selected member doesn't have card number. Please assign cardnumber to this member.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (string.IsNullOrWhiteSpace(CardNumber))
                {
                    MessageBox.Show("Please Enter Cardnumber first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
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
                //ParkingVouchers = new List<Voucher>();
                //string mcode = "";
                //var res=await ProductService.GetMcodeByDesca(SelectedMember.SchemeName);
                //if (res.status == "ok")
                //{
                //    mcode = res.result.ToString();
                //}
                //else
                //{
                //    throw new Exception("Product code for this scheme not found.");
                //}
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
                        VSales.BillTo = SelectedMember.MemberName;
                        VSales.BILLTOADD = SelectedMember.Address;
                        VSales.Save(tran);
                        foreach (TParkingSalesDetails PSD in VSDetailList)
                        {
                            PSD.BillNo = VSales.BillNo;
                            PSD.FYID = VSales.FYID;
                            PSD.Save(tran);
                        }
                        conn.Execute("update members set Expirydate=@Expirydate,ActivationDate=getdate(), SchemeId=@SchemeId where memberid=@memberid", new { Expirydate = VSales.ExpiryDate, memberid = SelectedMember.MemberId, SchemeId = SelectedMember.SchemeId }, transaction: tran);

                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                        GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "New", VCRHNO: VSales.BillNo, WorkDetail: "Bill No : " + VSales.BillNo);

                        //await SaveAccountBill(mcode);

                        //SyncFunctions.LogSyncStatus(tran, VSales.BillNo, GlobalClass.FYNAME);
                        //if (GenerateVoucher)
                        //{
                        //    vp = new wVoucherPrintProgress() { DataContext = this };
                        //    vp.Show();
                        //    await GenerateVouchers(tran);
                        //    vp.Hide();
                        //}
                        tran.Commit();
                        //if (!string.IsNullOrEmpty(SyncFunctions.username))
                        //{
                        //    SyncFunctions.SyncSalesData(SyncFunctions.getBillObject(VSales.BillNo), 1);
                        //}
                        MessageBox.Show("Card saved Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        if (!(string.IsNullOrEmpty(VSales.BillTo) || CustomerList.Any(x => x.Name == VSales.BillTo)))
                            CustomerList.Add(new Party { Name = VSales.BillTo, Address = VSales.BILLTOADD, PAN = VSales.BILLTOPAN });
                    }
                    //if (!string.IsNullOrEmpty(VSales.BillNo))
                    //{
                    PrintBill(VSales.BillNo, true);
                    ReActivateCard(SelectedMember.Barcode);
                    //    if (GenerateVoucher)
                    //    {
                    //        vp.Show();
                    //        await PrintVouchers(VSales.BillNo, true);
                    //        vp.Close();
                    //    }
                    //}
                    //GenerateVouchers();

                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        async Task<bool> SaveAccountBill(string mcode)
        {
            BillMain billMain = new BillMain();
            billMain.division = "MMX";
            billMain.terminal = GlobalClass.Terminal;
            billMain.trnuser = GlobalClass.User.UserName;
            billMain.trnmode = "Cash";
            billMain.trnac = "AT01002";
            billMain.parac = "AT01002";
            billMain.guid = Guid.NewGuid().ToString();
            billMain.voucherAbbName = VSales.GrossAmount > 5000 ? "TI" : "SI";
            billMain.Orders = "";
            billMain.ConfirmedBy = GlobalClass.User.UserName;
            billMain.tender = VSales.GrossAmount;


            foreach (var item in VSDetailList)
            {
                //var TaxedAmount = item.NetAmount;

                Product product = new Product
                {
                    mcode = mcode,
                    quantity = item.Quantity,
                    rate = item.Rate,
                };
                billMain.prodList.Add(product);
            }
            var result=await SaveBillAndPrint(billMain);
            return result;
        }
        private async Task<bool> SaveBillAndPrint(BillMain billMain)
        {
            var functionRes = await BillingService.SaveBill(billMain);
            if (functionRes.status == "1")
            {
                //PrintFunction.PrintBill(functionRes.result.ToString());
                //if (w != null)
                //{
                //    w.Close();
                //}

                return true;
            }
            else if (functionRes.status == "0")
            {
                MessageBox.Show(functionRes.result?.ToString());
                return false;
            }
            // to do log Couldn't save bill to database
            MessageBox.Show(functionRes.result.ToString());
            return false;
        }

        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("select * from DeviceList"));
            }
        }
        private void ReActivateCard(string barcode)
        {
            foreach (var device in DeviceList)
            {
                var zkem = new zkemkeeper.CZKEM();
                device.DeviceIp = device.DeviceIp.Trim();
                bool isValidIpA = UniversalStatic.ValidateIP(device.DeviceIp);
                if (!isValidIpA)
                {
                    MessageBox.Show($"Invalid Ip: {device.DeviceIp}");
                    continue;
                    //throw new Exception("The Device IP is invalid !!");
                }

                isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                if (!isValidIpA)
                {
                    //MessageBox.Show($"Couldn't connect to device: {device.DeviceIp}");
                    continue;
                    //throw new Exception("The device at " + device.DeviceIp + ":" + device.DevicePort + " did not respond!!");
                }
                if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                {
                    var cardId = GetEnrolledIdByCardNumber(barcode);

                    zkem.EnableUser(zkem.MachineNumber, cardId, zkem.MachineNumber, 10, true);
                }
            }
        }
        private int GetEnrolledIdByCardNumber(string barcode)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                var cardId = conn.Query<int>($"select cardid from dailycards where cardnumber='{barcode}'");
                return cardId.FirstOrDefault();
            }
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
                    //else
                    //{
                    //    pslip.InvoiceTitle = "INVOIVE";
                    //    pslip.Print();
                    //    GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                    //    GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                    //    if (MessageBox.Show("Would you like to reprint Vouchers as well?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    //    {
                    //        VoucherSelection vs = conn.Query<VoucherSelection>("SELECT MIN(VoucherNo) VNOFrom, MAX(VoucherNo) VNOTo FROM ParkingVouchers WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID }).FirstOrDefault();
                    //        wVoucherSelect wVS = new wVoucherSelect() { DataContext = vs };
                    //        wVS.ShowDialog();

                    //        vp = new wVoucherPrintProgress() { DataContext = this };
                    //        vp.Show();
                    //        await PrintVouchers(BillNo, false, vs);
                    //        vp.Close();
                    //    }
                    //}
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(Ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ExecuteUndo(object p)
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

        //private bool CanExecutePrint(object obj)
        //{
        //    return true;
        //}

        //private bool CanExecuteSave(object obj)
        //{
        //    return true;
        //}

        //private void ExecutePrint(object obj)
        //{
        //    throw new NotImplementedException();
        //}

        //private void ExecuteUndo(object obj)
        //{
        //    throw new NotImplementedException();
        //}

        //private void ExecuteSave(object obj)
        //{
        //    throw new NotImplementedException();
        //}

        //private void ExecuteNew(object obj)
        //{
        //    ExecuteUndo(null);
        //    InvoiceNo = GetInvoiceNo(InvoicePrefix);
        //    SetAction(ButtonAction.New);
        //    FocusedElement = (short)Focusable.Customer;
        //}

        //string GetInvoiceNo(string VNAME)
        //{
        //    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
        //    {
        //        string invoice = conn.ExecuteScalar<string>("SELECT CurNo FROM tblSequence WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID });
        //        if (string.IsNullOrEmpty(invoice))
        //        {
        //            conn.Execute("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID });
        //            invoice = "1";
        //        }
        //        return invoice;
        //    }
        //}
    }
}
