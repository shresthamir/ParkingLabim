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
    public partial class ucBirthDayReport : UserControl
    {
        vmMembersReport ViewModel;
        DateConverter nepDate;
        Style NumericColumn;
        private void txtFDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            txtFMiti.Text = nepDate.CBSDate(txtFDate.SelectedDate.Value);
        }

        private void txtFMiti_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                txtFDate.SelectedDate = nepDate.CADDate(txtFMiti.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid BS Date", "Daily Cash Collection Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtTDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            txtTMiti.Text = nepDate.CBSDate(txtTDate.SelectedDate.Value);
        }

        private void txtTMiti_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                txtFDate.SelectedDate = nepDate.CADDate(txtTMiti.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid BS Date", "Daily Cash Collection Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public ucBirthDayReport()
        {

            InitializeComponent();

            nepDate = new DateConverter(GlobalClass.TConnectionString);

            txtTDate.SelectedDate = DateTime.Today;
            txtFDate.SelectedDate = DateTime.Today;

            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "MemberId", Binding = new Binding("Column1"), Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "MemberName", Binding = new Binding("Column2"), Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Address", Binding = new Binding("Column3"), Width = 100 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Mobile", Binding = new Binding("Column4"), Width = 100 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "SchemeName", Binding = new Binding("Column5"), Width = 200 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column6"), Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "ActivationDate", Binding = new Binding("Date1") { StringFormat = "MM/dd/yyyy hh:mm:ss tt" }, Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "ExpiryDate", Binding = new Binding("Date2") { StringFormat = "MM/dd/yyyy hh:mm:ss tt" }, Width = 150 });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "CardNumber", Binding = new Binding("Column7"), Width = 150, CellStyle = NumericColumn });
            dgDailySales.Columns.Add(new DataGridTextColumn { Header = "DOB", Binding = new Binding("Date3") { StringFormat = "MM/dd/yyyy hh:mm:ss tt" }, Width = 150 });

            this.DataContext = ViewModel = new vmMembersReport();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dgDailySales.Columns.Clear();

                string SQL;
                //if (cmbFilter.SelectedIndex == 0)
                //{
                SQL = @"select m.MemberId Column1, Membername Column2,Address Column3,Mobile Column4,sd.description Column5,sd.rate Column6, s.TDate Date1, m.ExpiryDate Date2, Barcode Column7 from members M join membershipscheme MS on m.schemeid=ms.schemeid join parkingsales s on m.memberid=s.memberid join ParkingSalesDetails sd on s.billno=sd.billno where m.memberid=@memberid or m.membername=@memberid or m.barcode=@memberid
";
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "MemberId", Binding = new Binding("Column1"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "MemberName", Binding = new Binding("Column2"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Address", Binding = new Binding("Column3"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Mobile", Binding = new Binding("Column4"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "SchemeName", Binding = new Binding("Column5"), Width = 200 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column6"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "ActivationDate", Binding = new Binding("Date1") { StringFormat = "MM/dd/yyyy hh:mm:ss tt" }, Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "ExpiryDate", Binding = new Binding("Date2") { StringFormat = "MM/dd/yyyy hh:mm:ss tt" }, Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "CardNumber", Binding = new Binding("Column7"), Width = 150, CellStyle = NumericColumn });

                ViewModel.LoadSummaryReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value, SQL, txtMemberId.Text);
                //}

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void Print_Click(object sender, RoutedEventArgs e)
        {
            List<ExcelHeader> headers = new List<ExcelHeader>();
            headers.Add(new ExcelHeader { Header = GlobalClass.CompanyName.ToUpper(), FontSize = 14, IsBold = true, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            headers.Add(new ExcelHeader { Header = "Members Report -" + txtMemberId.Text, FontSize = 14, IsBold = false, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            headers.Add(new ExcelHeader { Header = txtFMiti.Text + " - " + txtTMiti.Text, FontSize = 12, IsBold = false, HorizontalAllignment = Microsoft.Office.Interop.Excel.Constants.xlCenter });
            DataGridExport.ExportDataGrid(dgDailySales, headers);
        }
    }

    class vmBirthDayReport : BaseViewModel
    {
        private ObservableCollection<DataItem> _ReportSource;
        //public CollectionViewSource cvs { get { return _cvs; } set { _cvs = value; OnPropertyChanged("cvs"); } }
        public ObservableCollection<DataItem> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }

        public vmBirthDayReport()
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

        public void LoadSummaryReport(DateTime FDate, DateTime TDate, string SQL, string memberId)
        {

            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    ReportSource = new ObservableCollection<DataItem>(conn.Query<DataItem>(SQL, new { FDATE = FDate, TDATE = TDate, memberId = memberId }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sales Report", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }




}
