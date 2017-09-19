using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ParkingManagement.Library;
using System.Threading;
using System.Windows;
using ParkingManagement.Forms;

namespace ParkingManagement.ViewModel
{
    class ComplementaryVoucherGeneratorViewModel : BaseViewModel
    {
        private int _TotQty;
        private string _GenCount;
        private decimal _Progress;
        VoucherType _SelectedVoucherType;
        wVoucherPrintProgress vp;
        private List<Voucher> ParkingVouchers;

        public int TotQty { get { return _TotQty; } set { _TotQty = value; OnPropertyChanged("TotQty"); } }
        public string GenCount { get { return _GenCount; } set { _GenCount = value; OnPropertyChanged("GenCount"); } }
        public decimal Progress { get { return _Progress; } set { _Progress = value; OnPropertyChanged("Progress"); } }
        public VoucherType SelectedVoucherType { get { return _SelectedVoucherType; } set { _SelectedVoucherType = value; OnPropertyChanged("SelectedVoucherType"); } }
        public List<VoucherType> VTypeList { get; set; }

        public ComplementaryVoucherGeneratorViewModel()
        {
            try
            {
                EntryPanelEnabled = true;
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    VTypeList = new List<VoucherType>(conn.Query<VoucherType>("SELECT VoucherId, VoucherName, Rate, Value, ValidStart, ValidEnd, Validity, VoucherInfo, SkipVoucherGeneration FROM VoucherTypes WHERE ISNULL(SkipVoucherGeneration, 0) = 0"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave(object obj)
        {
            return SelectedVoucherType != null && TotQty > 0;
        }

        private async void ExecuteSave(object obj)
        {
            try
            {
                EntryPanelEnabled = false;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        string billNo = "CC" + GetInvoiceNo("CC", tran);
                        vp = new wVoucherPrintProgress() { DataContext = this };
                        vp.Show();
                        await GenerateVouchers(tran, billNo);
                        vp.Hide();
                        conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = "CC", FYID = GlobalClass.FYID }, transaction: tran);
                        tran.Commit();

                        vp.Show();
                        await PrintVouchers(billNo);
                        vp.Close();
                    }
                }
                TotQty = 0;
                SelectedVoucherType = null;
                EntryPanelEnabled = true;
            }
            catch (Exception ex)
            {

            }
        }

        private async Task GenerateVouchers(SqlTransaction tran, string BillNo)
        {
            string Status = "{0} of " + TotQty.ToString() + " Done";

            await Task.Run(() =>
            {
                for (int i = 1; i <= TotQty; i++)
                {
                    VoucherType vt = SelectedVoucherType;

                    Voucher v = new Voucher()
                    {
                        BillNo = BillNo,
                        ExpDate = DateTime.Today.AddDays(vt.Validity),
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
                   // ParkingVouchers.Add(v);
                    GenCount = string.Format(Status, i);
                    Progress = i / TotQty * 100;
                }
            });
        }

        private async Task PrintVouchers(string BillNo)
        {
            Progress = 0;
            int counter = 1;
            List<Voucher> Barcodes = new List<Voucher>();

            await Task.Run(() =>
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    var Vouchers = conn.Query<Voucher>("SELECT VoucherNo, VoucherInfo VoucherName, PV.VoucherId, Barcode  FROM ParkingVouchers PV JOIN VoucherTypes VT ON PV.VoucherId = VT.VoucherId WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
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
    }
}
