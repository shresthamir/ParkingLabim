using DateFunction;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Library.ValueConverter;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
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
using Dapper;
namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for ucDailySalesReport.xaml
    /// </summary>
    public partial class ucParkingReport : UserControl
    {
        vmParkingReports ViewModel;
        DateConverter nepDate;
        Style NumericColumn;
        private void txtFDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            txtFMiti.Text = nepDate.CBSDate(txtFDate.SelectedDate.Value);
        }

        private void txtFMiti_LostFocus(object sender, RoutedEventArgs e)
        {
            if (nepDate.BSValidate(txtFMiti.Text))
                txtFDate.SelectedDate = nepDate.CADDate(txtFMiti.Text);
            else
                MessageBox.Show("Invalid BS Date", "Daily Cash Collection Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void txtTDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            txtTMiti.Text = nepDate.CBSDate(txtTDate.SelectedDate.Value);
        }

        private void txtTMiti_LostFocus(object sender, RoutedEventArgs e)
        {
            if (nepDate.BSValidate(txtTMiti.Text))
                txtTDate.SelectedDate = nepDate.CADDate(txtTMiti.Text);
            else
                MessageBox.Show("Invalid BS Date", "Daily Cash Collection Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public ucParkingReport()
        {

            InitializeComponent();

            nepDate = new DateConverter(GlobalClass.TConnectionString);

            txtTDate.SelectedDate = DateTime.Today;
            txtFDate.SelectedDate = DateTime.Today;

            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In", Binding = new Binding("Date1"){StringFormat = "MM/dd/yyyy hh:mm tt"}, Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out", Binding = new Binding("Date2"){StringFormat = "MM/dd/yyyy hh:mm tt"}, Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Column3"), Width = 100 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column4"), Width = 100 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column6"), Width = 200 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Party", Binding = new Binding("Column5"), Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "User", Binding = new Binding("Column7"), Width = 200 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1"){StringFormat = "#0.00"}, Width = 150, CellStyle = NumericColumn });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });

            this.DataContext = ViewModel = new vmParkingReports();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dgDailySales.Columns.Clear();

                string SQL;
                if (cmbFilter.SelectedIndex == 0)
                {
                    SQL = @"SELECT CAST(ROW_NUMBER() OVER(ORDER BY (PID.InDate + CAST(PID.InTime AS datetime))) AS Int) Int1, PID.InDate + CAST(PID.InTime AS datetime) Date1, 
                            PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, IU.FullName Column2,
                            PID.PlateNo Column3, POD.OutDate + CAST(POD.OutTime AS DATETIME) Date2, PID.InDate Date3, 
                            POD.OutMiti + ' ' + CAST(POD.OutTime AS VARCHAR) Column4,
                            POD.Interval Column5, VT.[Description] Column6, U.FullName Column7 FROM ParkingInDetails PID
                            INNER JOIN VehicleType VT ON VT.VTypeID = PID.VehicleType
                            INNER JOIN Users IU ON IU.[UID] = PID.[UID]
							LEFT OUTER JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND PID.FYID = POD.FYID
							LEFT OUTER JOIN Users U ON U.[UID] = POD.[UID]
                            WHERE (PID.InDate BETWEEN @FDATE AND @TDATE)
                            ORDER BY Date1";
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "SNo.", Binding = new Binding("Int1"), Width = 50, CellStyle = NumericColumn });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In Date", Binding = new Binding("Date1") { StringFormat = "MM/dd/yyyy hh:mm tt" }, Width = 130 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In User", Binding = new Binding("Column2"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column6"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Plate No", Binding = new Binding("Column3"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out Date", Binding = new Binding("Date2") { StringFormat = "MM/dd/yyyy hh:mm tt" }, Width = 130 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out User", Binding = new Binding("Column7"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Column5"), Width = 100 });
                    ViewModel.LoadSummaryReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value, SQL);
                }
                else if (cmbFilter.SelectedIndex == 1)
                {
                    SQL = @"SELECT CAST(ROW_NUMBER() OVER(ORDER BY Date1) AS Int) Int1, *, DATEDIFF(Mi,Date1,(SELECT GETDATE())) Int2 FROM (
                            SELECT PID.InDate + CAST(PID.InTime AS datetime) Date1, PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, IU.FullName Column2, PID.InDate Date3,
                            PID.PlateNo Column3, VT.[Description] Column6,POD.PID Int3 FROM ParkingInDetails PID
                            INNER JOIN VehicleType VT ON VT.VTypeID = PID.VehicleType
                            INNER JOIN Users IU ON IU.[UID] = PID.[UID] 
                            LEFT JOIN ParkingOutDetails POD ON POD.PID=PID.PID AND POD.FYID = PID.FYID
                            WHERE (PID.InDate BETWEEN @FDATE AND @TDATE)) TEMP WHERE Int3 IS NULL";
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "SNo.", Binding = new Binding("Int1"), Width = 50, CellStyle = NumericColumn });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In Date", Binding = new Binding("Date1") { StringFormat = "MM/dd/yyyy hh:mm tt" }, Width = 130 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In User", Binding = new Binding("Column2"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column6"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Plate No", Binding = new Binding("Column3"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Int2") { Converter = new MinToHHMMConverter() }, Width = 100 });
                    ViewModel.LoadSummaryReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value, SQL);
                }
                else if (cmbFilter.SelectedIndex == 2)
                {
                    SQL = @"SELECT CAST(ROW_NUMBER() OVER(ORDER BY (PID.InDate + CAST(PID.InTime AS datetime))) AS Int) Int1, PID.InDate + CAST(PID.InTime AS datetime) Date1, 
                            PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, IU.FullName Column2,
                            PID.PlateNo Column3, POD.OutDate + CAST(POD.OutTime AS DATETIME) Date2, PID.InDate Date3, 
                            POD.OutMiti + ' ' + CAST(POD.OutTime AS VARCHAR) Column4,
                            POD.Interval Column5, VT.[Description] Column6, U.FullName Column7 FROM ParkingInDetails PID
                            INNER JOIN VehicleType VT ON VT.VTypeID = PID.VehicleType
                            INNER JOIN Users IU ON IU.[UID] = PID.[UID]
							INNER JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND POD.FYID = PID.FYID
							INNER JOIN Users U ON U.[UID] = POD.[UID]
                            WHERE (PID.InDate BETWEEN @FDATE AND @TDATE)
                            ORDER BY Date1";
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "SNo.", Binding = new Binding("Int1"), Width = 50, CellStyle = NumericColumn });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In Date", Binding = new Binding("Date1") { StringFormat = "MM/dd/yyyy hh:mm tt" }, Width = 130 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In User", Binding = new Binding("Column2"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column6"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Plate No", Binding = new Binding("Column3"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out Date", Binding = new Binding("Date2") { StringFormat = "MM/dd/yyyy hh:mm tt" }, Width = 130 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out User", Binding = new Binding("Column7"), Width = 150 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Column5"), Width = 100 });
                    ViewModel.LoadSummaryReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value, SQL);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
           

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            List<ExcelHeader> headers = new List<ExcelHeader>();
            headers.Add(new ExcelHeader { Header = GlobalClass.CompanyName.ToUpper(), FontSize = 14, IsBold = true, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            headers.Add(new ExcelHeader { Header = "Parking Report -" + cmbFilter.Text, FontSize = 14, IsBold = false, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            headers.Add(new ExcelHeader { Header = txtFMiti.Text + " - " + txtTMiti.Text, FontSize = 12, IsBold = false, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            DataGridExport.ExportDataGrid(dgDailySales, headers);
        }
    }

    class vmParkingReports : BaseViewModel
    {
        private ObservableCollection<DataItem> _ReportSource;
        //public CollectionViewSource cvs { get { return _cvs; } set { _cvs = value; OnPropertyChanged("cvs"); } }
        public ObservableCollection<DataItem> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }

        public vmParkingReports()
        {

        }

        public void LoadDetailsReport(DateTime FDate, DateTime TDate)
        {
            string strSQL;
            try
            {
                strSQL = @"SELECT PID.InDate + CAST(PID.InTime AS datetime) Date1, PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, POD.OutDate Date3,
                            PID.PlateNo Column2, POD.OutDate + CAST(POD.OutTime AS DATETIME) Date2, POD.OutMiti + ' ' + CAST(POD.OutTime AS VARCHAR) Column8,
                            POD.Interval Column3, VT.[Description] Column4, P.PartyName Column5, RM.RateDescription Column6, POD.ChargedAmount Decimal2,
                            POD.CashAmount Decimal1, U.FullName Column7 FROM ParkingInDetails PID 
                            INNER JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND POD.FYID = PID.FYID
                            INNER JOIN VehicleType VT ON VT.VehicleTypeID = PID.VehicleTypeID
                            INNER JOIN RateMaster RM ON RM.Rate_ID = POD.Rate_ID
                            INNER JOIN Users U ON U.[UID] = POD.[UID]
                            LEFT JOIN Party P ON P.Party_ID=POD.Party_ID
                            WHERE ( POD.OutDate BETWEEN @FDATE AND @TDATE)
                            ORDER BY Date2,Date1,Column7,Column4";
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ReportSource = new ObservableCollection<DataItem>(conn.Query<DataItem>(strSQL, new { FDATE = FDate, TDATE = TDate }));
                    // cvs = new CollectionViewSource { Source = show };

                    // cvs.GroupDescriptions.Add(new PropertyGroupDescription("EventID"));
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void LoadSummaryReport(DateTime FDate, DateTime TDate, string SQL)
        {

            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ReportSource = new ObservableCollection<DataItem>(conn.Query<DataItem>(SQL, new { FDATE = FDate, TDATE = TDate }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sales Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }

   


}
