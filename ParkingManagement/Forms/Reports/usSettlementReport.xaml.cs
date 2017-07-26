
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Library;
using Dapper;
using ParkingManagement.Models;

namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for Report_Dialog.xaml
    /// </summary>
    public partial class ucSettlementReport : UserControl
    {


        public ucSettlementReport()
        {

            InitializeComponent();
            this.DataContext = new vmSettlementReport() { Report = Report };
        }
    }


    class vmSettlementReport : BaseViewModel
    {
        public Syncfusion.UI.Xaml.Grid.SfDataGrid Report { get; set; }
        private ObservableCollection<ReportModel> _ReportSource;
        private DateTime _TDate;
        private DateTime _FDate;
        private List<User> _UserList;
        private int _SelectedUser;
        public byte ReportFlag { get; set; } // 0:Reprint Log, 1: ANNEX7, 2: ABB SALES REGISTER SUMMARY
        public ObservableCollection<ReportModel> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }
        public List<User> UserList { get { return _UserList; } set { _UserList = value; OnPropertyChanged("UserList"); } }
        public DateTime TDate { get { return _TDate; } set { _TDate = value; OnPropertyChanged("TDate"); } }
        public DateTime FDate { get { return _FDate; } set { _FDate = value; OnPropertyChanged("FDate"); } }
        public int SelectedUser { get { return _SelectedUser; } set { _SelectedUser = value; OnPropertyChanged("SelectedUser"); } }
        public vmSettlementReport()
        {
            this.MessageBoxCaption = "Parking Management";
            FDate = DateTime.Today.Date;
            TDate = DateTime.Today.Date;
            PrintPreviewCommand = new RelayCommand(ExecutePrintPreview, CanExecutePrintExport);
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrintExport);
            LoadData = new RelayCommand(LoadReport);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecutePrintExport);
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    UserList = conn.Query<User>(@"
                            SELECT * FROM
                            (
                                SELECT DISTINCT U.UID, UserName FROM Users U JOIN ParkingSales PS ON U.UID = PS.UID

                                UNION ALL

                                SELECT 0, ' All User'
                            ) A ORDER BY UserName").ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ExecuteExport(object obj)
        {
            GlobalClass.ReportName = GetReportName();

            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));

            wExportFormat ef = new wExportFormat(Report);
            ef.ShowDialog();
        }

        private bool CanExecutePrintExport(object obj)
        {
            return ReportSource != null && ReportSource.Count > 0;
        }


        private void ExecutePrintPreview(object obj)
        {
            GlobalClass.ReportName = GetReportName();

            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));

            Report.PrintSettings.PrintPageMargin = new Thickness(30);
            Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;
            Report.PrintSettings.PrintPageOrientation = PrintOrientation.Landscape;
            Report.ShowPrintPreview();
        }
        private void ExecutePrint(object obj)
        {

            GlobalClass.ReportName = GetReportName();
            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));

            Report.PrintSettings.PrintPageMargin = new Thickness(30);
            Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;                      
            Report.Print();
        }

        private void LoadReport(object param)
        {
            string strSql = string.Empty;
            try
            {
                if (ReportFlag == 0)
                    strSql = string.Format(@"SELECT SETTLEMENT_ID REPRINTNO, U.UserName PRINTED_BY, CS.TRNDATE BILL_DATE, CS.TRNTIME PRINTED_TIME, CS.AMOUNT, CS.CollectionAmount TAXABLE_AMOUNT, CS.AMOUNT - CS.CollectionAmount TAX_AMOUNT
                        FROM CashSettlement CS 
                        JOIN TERMINALS T ON CS.TERMINAL_CODE = T.TERMINAL_CODE
                        JOIN USERS U ON U.UID = CS.SETTLED_UID WHERE CS.TRNDATE BETWEEN '{0}' AND '{1}'{2}", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"),
                        ((SelectedUser > 0) ? " AND CS.SETTLED_UID = " + SelectedUser : string.Empty));



                LoadColumns();
                var data = GetDataTable(strSql);
                if (data != null && data.Count() == 0)
                    MessageBox.Show("NoData");
                else
                    ReportSource = new ObservableCollection<ReportModel>(data);
                GlobalClass.SetUserActivityLog(GetReportName(), "View", string.Empty, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void LoadColumns()
        {

            Report.Columns.Clear();
            Report.TableSummaryRows.Clear();
            Report.StackedHeaderRows.Clear();
            if (ReportFlag == 0)
            {

                Report.Columns.Add(new GridTextColumn { HeaderText = "Id", DisplayBinding = new Binding("REPRINTNO"), Width = 80 });                
                Report.Columns.Add(new GridTextColumn { HeaderText = "User", DisplayBinding = new Binding("PRINTED_BY") , Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Date", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Time", DisplayBinding = new Binding("PRINTED_TIME"), Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Settled Amount", DisplayBinding = new Binding("AMOUNT") { StringFormat = "#0.00" }, Width = 120, TextAlignment=TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Collected Amount", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#0.00" }, Width = 120, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Excess/Short", DisplayBinding = new Binding("TAX_AMOUNT") { StringFormat = "#0.00" }, Width = 100, TextAlignment = TextAlignment.Right });
                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false, Title = "Total" };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "AMOUNT", Name = "TCUSTOMER_NAME", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TDISCOUNT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_AMOUNT", Name = "TEXORT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }

        }

        string GetReportName()
        {
            if (ReportFlag == 0)
                return "Settlement Report";
            return "Settlement Report";
        }


        public IEnumerable<ReportModel> GetDataTable(string strSql)
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
    }


}


