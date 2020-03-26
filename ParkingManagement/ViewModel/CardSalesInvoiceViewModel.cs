using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
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
    public class CardSalesInvoiceViewModel: VoucherSalesInvoiceVM
    {
        
        private ObservableCollection<Member> _MemberList;
        private ObservableCollection<MembershipScheme> _SchemeList;
        private MembershipScheme _SelectedScheme;
        private Member _SelectedMember;

        public ObservableCollection<Member> MemberList { get { return _MemberList; } set { _MemberList = value; OnPropertyChanged("MemberList"); } }
        public Member SelectedMember { get { return _SelectedMember; } set { _SelectedMember = value; OnPropertyChanged("SelectedMember"); } }
        public ObservableCollection<MembershipScheme> SchemeList { get { return _SchemeList; } set { _SchemeList = value; OnPropertyChanged("SchemeList"); } }
        public MembershipScheme SelectedScheme { get { return _SelectedScheme; } set { _SelectedScheme = value; OnPropertyChanged("SelectedScheme"); } }


        public new RelayCommand AddVoucherCommand { get { return new RelayCommand(AddCard, CanAddCard); } }

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
            if (VSDetail.ProdId == 0)
            {
                MessageBox.Show("Please Select Membership Scheme first.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (VSDetailList.Any(x => x.ProdId == VSDetail.ProdId))
            {
                MessageBox.Show("Selected Card is already added.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if(SelectedScheme is null)
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
            VSDetailList.Add(vsDetail);
            VSDetail = new TParkingSalesDetails();
            FocusedElement = (short)Focusable.VoucherType;
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
                MemberList = new ObservableCollection<Member>(conn.Query<Member>("SELECT * FROM Members"));
                SchemeList = new ObservableCollection<MembershipScheme>(conn.Query<MembershipScheme>("SELECT * FROM MembershipScheme"));
            }

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
                //ParkingVouchers = new List<Voucher>();
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
                        conn.Execute("update members set Expirydate=@Expirydate,ActivationDate=getdate() where memberid=@memberid", new { Expirydate= VSales.ExpiryDate, memberid=SelectedMember.MemberId }, transaction: tran);

                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                        GlobalClass.SetUserActivityLog("Voucher Sales Invoice", "New", VCRHNO: VSales.BillNo, WorkDetail: "Bill No : " + VSales.BillNo);

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
                    //    PrintBill(VSales.BillNo, true);
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
