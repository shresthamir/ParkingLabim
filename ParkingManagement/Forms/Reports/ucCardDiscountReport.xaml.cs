using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ParkingManagement.Library;
using Dapper;
using Syncfusion.UI.Xaml.Grid;
using ParkingManagement.Models;

namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for ucCardDiscountReport.xaml
    /// </summary>
    public partial class ucCardDiscountReport : UserControl
    {
        DateConverter nepDate;

        public vmCardDiscountReport ViewModel { get; set; }
        public ucCardDiscountReport()
        {
            InitializeComponent();
            //this.lbSummary.Visibility = this.cmbSummary.Visibility = (Flag == 1) ? Visibility.Visible : Visibility.Collapsed;
            this.DataContext = ViewModel = new vmCardDiscountReport();
            nepDate = new DateConverter(GlobalClass.TConnectionString);

            //txtTDate.SelectedDate = nepDate.GetLastDateOfBSMonth(DateTime.Today);
            //txtFDate.SelectedDate = nepDate.GetFirstDateOfBSMonth(DateTime.Today);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Report.Columns.Clear();
            if (rbDetails.IsChecked.Value)
            {
                Report.Columns.Add(new DataGridTextColumn { Header = "BILL_NO", Binding = new Binding("Column1"), Width = 150 });//CellContentStringFormat = "{0:MM/dd/yyyy hh:mm tt}",
                Report.Columns.Add(new DataGridTextColumn { Header = "BILL_DATE", Binding = new Binding("Date1"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy hh:mm tt}",
                Report.Columns.Add(new DataGridTextColumn { Header = "PRINTED_TIME", Binding = new Binding("Column3"), Width = 100 });
                Report.Columns.Add(new DataGridTextColumn { Header = "Remarks", Binding = new Binding("Column4"), Width = 100 });
                Report.Columns.Add(new DataGridTextColumn { Header = "Discount", Binding = new Binding("Column5"), Width = 200 });
                Report.Columns.Add(new DataGridTextColumn { Header = "BillTo", Binding = new Binding("Column6"), Width = 200 });
                Report.Columns.Add(new DataGridTextColumn { Header = "PRINTED_BY", Binding = new Binding("Column7") { StringFormat = "#0.00" }, Width = 150 });// CellContentStringFormat = "{0:#0.00}",

                ViewModel.LoadDetailsReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value);
            }

        }

        private void Report_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class vmCardDiscountReport : BaseViewModel
    {
        public Syncfusion.UI.Xaml.Grid.SfDataGrid Report { get; set; }
        private ObservableCollection<DataItem> _ReportSource;
        private DateTime _TDate;
        private DateTime _FDate;
        private bool _ShowSISummary;
        private string _summaryType = "REMARKS";
        private int _SelectedVoucher;
        public RelayCommand LoadData { get; set; }

        public byte ReportFlag { get; set; } // 0:Reprint Log, 1: ANNEX7, 2: ABB SALES REGISTER SUMMARY

        public ObservableCollection<DataItem> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }
        public string SummaryType { get { return _summaryType; } set { _summaryType = value; OnPropertyChanged("SummaryType"); } }
        public DateTime TDate { get { return _TDate; } set { _TDate = value; OnPropertyChanged("TDate"); } }
        public DateTime FDate { get { return _FDate; } set { _FDate = value; OnPropertyChanged("FDate"); } }

        public void LoadDetailsReport(DateTime FDate, DateTime TDate)
        {
            string strSQL;
            try
            {
               
                strSQL = string.Format(@"SELECT PSD.BillNo Column1, PS.TDate Date1, PS.TTime Column3, PSD.Remarks Column4,PSD.Discount Column5,PS.BillTo Column6,U.UserName Column7
                                            from ParkingSalesDetails PSD
                                            JOIN ParkingSales PS on PSD.BillNo = ps.BillNo
                                            JOIN USERS U ON U.UID = PS.UID WHERE PS.TDate BETWEEN '{0}' AND '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ReportSource = new ObservableCollection<DataItem>(conn.Query<DataItem>(strSQL));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Daily Sales Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSummaryReport(string SQL)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ReportSource = new ObservableCollection<DataItem>(conn.Query<DataItem>(SQL));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sales Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //public vmCardDiscountReport()
        //{
        //    LoadData = new RelayCommand(LoadReport);
        //}
        private void ExecuteExport(object obj)
        {
            //GlobalClass.ReportName = GetReportName();
            //GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));

            //wExportFormat ef = new wExportFormat(Report);
            //ef.ShowDialog();
        }
        //private void LoadReport(object param)
        //{
        //    string strSql = string.Empty;
        //    try
        //    {
        //        //VOUCHER DISCOUNT REPORT - DETAILS
        //        if (ReportFlag == 0)
        //            //strSql = string.Format(@"SELECT VSD.BillNo BILL_NO, CAST(CONVERT(VARCHAR(10),PV.ScannedTime, 101) AS DATETIME) BILL_DATE, RIGHT( CONVERT(VARCHAR, ScannedTime, 0),7) PRINTED_TIME, VSD.VoucherNo REF_NO, PV.VoucherName REMARKS, VSD.DiscountAmount DISCOUNT, PSV.BillTo CUSTOMER_NAME, U.UserName PRINTED_BY 
        //            //                        FROM VoucherDiscountDetail VSD 
        //            //                        JOIN ParkingVouchers PV ON VSD.VoucherNo = PV.VoucherNo --AND PV.FYID = VSD.FYID
        //            //                        LEFT JOIN ParkingSales PSV ON PV.BillNo = PSV.BillNo AND PSV.FYID = PV.FYID 
        //            //                        JOIN USERS U ON U.UID = VSD.UID WHERE ScannedTime BETWEEN '{0}' AND '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy") + " 23:59:59", (SelectedVoucher == 0) ? string.Empty : " AND PV.VoucherId = " + SelectedVoucher);

        //            strSql = string.Format(@"SELECT PSD.BillNo BILL_NO, PS.TDate BILL_DATE, PS.TTime PRINTED_TIME, PSD.Remarks,PSD.Discount,PS.BillTo,U.UserName PRINTED_BY
        //                                    from ParkingSalesDetails PSD
        //                                    JOIN ParkingSales PS on PSD.BillNo = ps.BillNo
        //                                    JOIN USERS U ON U.UID = PS.UID WHERE PS.TDate BETWEEN '{0}' AND '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
        //        //VOUCHER DISCOUNT REPORT - SUMMARY
        //        //else if (ReportFlag == 1)
        //        //{

        //        //    strSql = string.Format(@"SELECT BILL_DATE, {0}, COUNT(*) REPRINTNO, SUM(DISCOUNT) DISCOUNT FROM
        //        //                                (
        //        //                                SELECT CAST(CONVERT(VARCHAR(10),PV.ScannedTime, 101) AS DATETIME) BILL_DATE, PV.VoucherName REMARKS, VSD.DiscountAmount DISCOUNT, PSV.BillTo CUSTOMER_NAME, U.UserName PRINTED_BY 
        //        //                                FROM VoucherDiscountDetail VSD 
        //        //                                JOIN ParkingVouchers PV ON VSD.VoucherNo = PV.VoucherNo -- AND PV.FYID = VSD.FYID
        //        //                                LEFT JOIN ParkingSales PSV ON PV.BillNo = PSV.BillNo AND PSV.FYID = PV.FYID 
        //        //                                JOIN USERS U ON U.UID = VSD.UID WHERE ScannedTime BETWEEN '{1}' AND '{2}'{3}
        //        //                                ) a GROUP BY BILL_DATE, {0} ORDER BY BILL_DATE", SummaryType, FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy") + " 23:59:59", (SelectedVoucher == 0) ? string.Empty : " AND PV.VoucherId = " + SelectedVoucher);
        //        //}


        //        LoadColumns();
        //        var data = GetDataTable(strSql);
        //        if (data != null && data.Count() == 0)
        //            MessageBox.Show("NoData");
        //        else
        //            ReportSource = new ObservableCollection<ReportModel>(data);
        //        GlobalClass.SetUserActivityLog(GetReportName(), "View", string.Empty, string.Empty, string.Empty);
        //    }
        //    catch (Exception Ex)
        //    {
        //        MessageBox.Show(Ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
           
        //}

        string GetReportName()
        {
            if (ReportFlag == 0)
                return "Voucher Redeem Report - Details";
            else if (ReportFlag == 1)
                return "Voucher Redeem Report - Summary";
            return "Voucher Redeem Report - Details";
        }

        private IEnumerable<ReportModel> GetDataTable(string strSql)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    return conn.Query<ReportModel>(strSql);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void LoadColumns()
        {

            Report.Columns.Clear();
            Report.TableSummaryRows.Clear();
            Report.StackedHeaderRows.Clear();
            if (ReportFlag == 0)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = "Bill No", DisplayBinding = new Binding("BILL_NO"), Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Date", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Time", DisplayBinding = new Binding("PRINTED_TIME"), Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Voucher No", DisplayBinding = new Binding("REF_NO"), Width = 80 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Voucher Name", DisplayBinding = new Binding("REMARKS"), Width = 280 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Customer", DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 250 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "User Name", DisplayBinding = new Binding("PRINTED_BY"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Discount", DisplayBinding = new Binding("DISCOUNT") { StringFormat = "#0.00" }, Width = 100, TextAlignment = TextAlignment.Right });
                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "CUSTOMER_NAME", Name = "TCUSTOMER_NAME", SummaryType = Syncfusion.Data.SummaryType.CountAggregate, Format = "{Count}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "DISCOUNT", Name = "TDISCOUNT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
            else if (ReportFlag == 1)
            {
                string Header = (SummaryType == "REMARKS") ? "Voucher Name" : ((SummaryType == "CUSTOMER_NAME") ? "Customer" : "User Name");

                Report.Columns.Add(new GridTextColumn { HeaderText = "Date", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 150 });
                Report.Columns.Add(new GridTextColumn { HeaderText = Header, DisplayBinding = new Binding(SummaryType), Width = 600 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "No of Voucher", DisplayBinding = new Binding("REPRINTNO"), Width = 150 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Discount", DisplayBinding = new Binding("DISCOUNT") { StringFormat = "#0.00" }, Width = 150, TextAlignment = TextAlignment.Right });
                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "REPRINTNO", Name = "TREPRINTNO", SummaryType = Syncfusion.Data.SummaryType.Int32Aggregate, Format = "{Sum}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "DISCOUNT", Name = "TDISCOUNT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
        }
    }
}
