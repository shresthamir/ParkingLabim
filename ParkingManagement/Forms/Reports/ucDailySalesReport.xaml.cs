using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Dapper;
namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for ucDailySalesReport.xaml
    /// </summary>
    public partial class ucDailySalesReport : UserControl
    {
        vmDailySales ViewModel;
        DateConverter nepDate;
        Style NumericColumn;
     
        public ucDailySalesReport()
        {
            try
            {
                InitializeComponent();

                nepDate = new DateConverter(GlobalClass.TConnectionString);

                NumericColumn = new Style();
                NumericColumn.TargetType = typeof(DataGridCell);
                NumericColumn.Setters.Add(new Setter { Property = DataGridCell.HorizontalAlignmentProperty, Value = HorizontalAlignment.Right });





                txtFDate.SelectedDate = DateTime.Now;
                txtTDate.SelectedDate = nepDate.GetLastDateOfBSMonth(DateTime.Today);
                //txtFDate.SelectedDate = nepDate.GetFirstDateOfBSMonth(DateTime.Today);

                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In", Binding = new Binding("Date1"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out", Binding = new Binding("Date2"), Width = 150, }); // CellContentStringFormat = "{0:MM/dd/yyyy hh:mm tt}",
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Column3"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column4"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column6"), Width = 200 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Party", Binding = new Binding("Column5"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "User", Binding = new Binding("Column7"), Width = 200 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") {StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",

                this.DataContext = ViewModel = new vmDailySales();
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Sales Report",MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            dgDailySales.Columns.Clear();
            if (rbDetails.IsChecked.Value)
            {
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "In", Binding = new Binding("Date1"), Width = 150 });//CellContentStringFormat = "{0:MM/dd/yyyy hh:mm tt}",
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Out", Binding = new Binding("Date2"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy hh:mm tt}",
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Interval", Binding = new Binding("Column3"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column4"), Width = 100 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column6"), Width = 200 });
                //dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Party", Binding = new Binding("Column5"), Width = 150 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "User", Binding = new Binding("Column7"), Width = 200 });
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",
                dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",

                ViewModel.LoadDetailsReport(txtFDate.SelectedDate.Value, txtTDate.SelectedDate.Value);
            }
            else
            {
                string SQL;
                if (cmdSummary.SelectedIndex == 0)
                {
                    SQL = string.Format(@"SELECT [OutDate] Date3 ,U.FullName Column1,Count(S.PID) Int1,SUM(ChargedAmount) Decimal1,SUM(CashAmount) Decimal2 FROM ParkingOutDetails S
                                            INNER JOIN Users U ON U.[UID] = S.[UID] WHERE (S.OuTDATE BETWEEN '{0}' AND '{1}')
                                            GRoup BY [OutDATE],U.FullName ORDER BY Date3, Column1", txtFDate.SelectedDate.Value.ToString("MM/dd/yyyy"), txtTDate.SelectedDate.Value.ToString("MM/dd/yyyy"));
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "TDate", Binding = new Binding("Date3"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "User", Binding = new Binding("Column1"), Width = 200 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding("Int1"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",
                    
                }
                else if (cmdSummary.SelectedIndex == 1)
                {
                    SQL = string.Format(@"SELECT [OutDate] Date3 ,T.[Description] Column1,Count(S.PID) Int1,SUM(ChargedAmount) Decimal1,SUM(CashAmount) Decimal2 FROM ParkingOutDetails S
                                            INNER JOIN ParkingInDetails SD ON SD.PID = S.PID AND SD.FYID = S.FYID
                                            INNER JOIN VehicleType T ON T.VTypeID = SD.VehicleType
                                            WHERE (S.OutDate BETWEEN '{0}' AND '{1}')
                                            GRoup BY [OutDATE],T.[Description] ORDER BY Date3, Column1", txtFDate.SelectedDate.Value.ToString("MM/dd/yyyy"), txtTDate.SelectedDate.Value.ToString("MM/dd/yyyy"));
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "TDate", Binding = new Binding("Date3"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Vehicle Type", Binding = new Binding("Column1"), Width = 200 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding("Int1"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",
                    
                }
                else if (cmdSummary.SelectedIndex == 2)
                {
                    SQL = string.Format(@"SELECT [OutDate] Date3 ,SD.RateDescription Column1,Count(PID) Int1,SUM(ChargedAmount) Decimal1,SUM(CashAmount) Decimal2 FROM ParkingOutDetails S
                                    INNER JOIN RateMaster SD ON SD.Rate_ID = S.Rate_ID
                                    WHERE (S.OutDate BETWEEN '{0}' AND '{1}')
                                    GRoup BY [OutDate],SD.RateDescription ORDER BY Date3, Column1", txtFDate.SelectedDate.Value.ToString("MM/dd/yyyy"), txtTDate.SelectedDate.Value.ToString("MM/dd/yyyy"));
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "TDate", Binding = new Binding("Date3"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Column1"), Width = 200 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding("Int1"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",

                    
                }
                else
                {
                    SQL = string.Format(@"SELECT [OutDate] Date3 ,P.PartyName Column1, Count(PID) Int1,SUM(PartyAmount) Decimal1 FROM ParkingOutDetails S
                                            INNER JOIN Party P ON P.Party_ID = S.Party_ID
                                            WHERE (S.OutDate BETWEEN '{0}' AND '{1}')
                                            GRoup BY [OutDate],P.PartyName ORDER BY Date3, Column1", txtFDate.SelectedDate.Value.ToString("MM/dd/yyyy"), txtTDate.SelectedDate.Value.ToString("MM/dd/yyyy"));
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "TDate", Binding = new Binding("Date3"), Width = 150 });// CellContentStringFormat = "{0:MM/dd/yyyy}",
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Party", Binding = new Binding("Column1"), Width = 200 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding("Int1"), Width = 100 });
                    dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });// CellContentStringFormat = "{0:#0.00}",
                }
                ViewModel.LoadSummaryReport(SQL);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {

            //dgDailySales.ExportToExcel("D:\\Sales.xlsx");
            var Headers = new List<ExcelHeader>();
            Headers.Add(new ExcelHeader { FontSize = 14, IsBold = true, Header = GlobalClass.CompanyName.ToUpper() });
            Headers.Add(new ExcelHeader { FontSize = 12, IsBold = false, Header = "Daily Sales Report" });
            Headers.Add(new ExcelHeader { FontSize = 12, IsBold = false, Header = "FROM : " + txtFMiti.Text + " TO: " + txtTMiti.Text });
            DataGridExport.ExportDataGrid(dgDailySales, Headers);

        }

        private void dgDailySales_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //e.Handled = true;
            //(this.DataContext as vmDailySales).Sort(e);
        }

        private void DateSelectionThisMonth_Click(object sender, RoutedEventArgs e)
        {
            txtFDate.SelectedDate = DateTime.Parse(DateTime.Today.Month.ToString().PadLeft(2, '0') + "/01/" + DateTime.Today.Year);
            txtTDate.SelectedDate = DateTime.Today;
            LoadReport();
        }

       

        private void DateSelectionThisWeek_Click(object sender, RoutedEventArgs e)
        {
            txtFDate.SelectedDate = DateTime.Today.Subtract(new TimeSpan(7, 0, 0, 0, 0));
            txtTDate.SelectedDate= DateTime.Today;
            LoadReport();

        }

        private void LoadReport()
        {
            dgDailySales.Columns.Clear();
            string SQL = string.Empty; ;
            
                SQL = string.Format(@"SELECT SUM(ChargedAmount) Decimal1,SUM(CashAmount) Decimal2 FROM ParkingOutDetails S
                                            INNER JOIN Users U ON U.[UID] = S.[UID] WHERE (S.OuTDATE BETWEEN '{0}' AND '{1}')",
                                            txtFDate.SelectedDate.Value.ToString("MM/dd/yyyy"), txtTDate.SelectedDate.Value.ToString("MM/dd/yyyy"));
               
                //dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Charged Amount", Binding = new Binding("Decimal1") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",
                //dgDailySales.Columns.Add(new DataGridTextColumn { Header = "Amount", Binding = new Binding("Decimal2") { StringFormat = "#0.00" }, Width = 150, CellStyle = NumericColumn });//CellContentStringFormat = "{0:#0.00}",

            ViewModel.LoadSummaryReport(SQL);


        }
        //void SetGroupHeader(DataGrid dg, params StatFields[] fields)
        // {
        //     GroupStyle gs = new GroupStyle();
        //     Style s = new Style(typeof(GroupItem));


        //     ControlTemplate ct = new ControlTemplate(typeof(GroupItem));
        //     var ex = new FrameworkElementFactory(typeof(Expander));

        //     StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };

        //     foreach (DataGridColumn dc in dg.Columns)
        //     {
        //         DataGridCell cell = new DataGridCell();
        //         cell.Width = dc.Width.Value;
        //         if(fields.Any(x=>x.FieldName.ToUpper()==dc.Header.ToString().ToUpper()))
        //         {
        //             var field = fields.FirstOrDefault(x => x.FieldName.ToUpper() == dc.Header.ToString().ToUpper());
        //             cell.Content = field.Content;
        //         }
        //         sp.Children.Add(cell);
        //     }
        //     ex.SetValue(Expander.HeaderTemplateProperty,sp);
        //     Expander exe = new Expander();

        //     ct.VisualTree = ex;
        //     s.Setters.Add(new Setter { Property = ContentTemplateProperty, Value = ct });
        //     gs.ContainerStyle=s;
        //     dg.GroupStyle.Add(gs);

        // }


    }

    class StatFields
    {
        public enum Type
        {
            Count, Sum, Average, GroupName
        }

        public string FieldName { get; set; }
        public Type FieldType { get; set; }

        public object Content { get; set; }
        public Binding binding { get; set; }

    }

    class vmDailySales : BaseViewModel
    {
        private ObservableCollection<DataItem> _ReportSource;
        //public CollectionViewSource cvs { get { return _cvs; } set { _cvs = value; OnPropertyChanged("cvs"); } }
        public ObservableCollection<DataItem> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }



        public vmDailySales()
        {

        }


        public void LoadDetailsReport(DateTime FDate, DateTime TDate)
        {
            string strSQL;
            try
            {
                //                strSQL = @"SELECT * FROM
                //                            (SELECT 'A' Column9, PID.InDate + CAST(PID.InTime AS datetime) Date1, PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, POD.OutDate Date3,
                //                            PID.PlateNo Column2, POD.OutDate + CAST(POD.OutTime AS DATETIME) Date2, POD.OutMiti + ' ' + CAST(POD.OutTime AS VARCHAR) Column8,
                //                            POD.Interval Column3, VT.[Description] Column4, P.PartyName Column5, RM.RateDescription Column6, POD.ChargedAmount Decimal2,
                //                            POD.CashAmount Decimal1, U.FullName Column7 FROM ParkingInDetails PID 
                //                            INNER JOIN ParkingOutDetails POD ON PID.PID = POD.PID
                //                            INNER JOIN VehicleType VT ON VT.VehicleTypeID = PID.VehicleTypeID
                //                            INNER JOIN RateMaster RM ON RM.Rate_ID = POD.Rate_ID
                //                            INNER JOIN Users U ON U.[UID] = POD.[UID]
                //                            LEFT JOIN Party P ON P.Party_ID=POD.Party_ID
                //                            WHERE ( POD.OutDate BETWEEN {0} AND {1})
                //                            UNION ALL 
                //                            SELECT 'B' Column9, '01/01/1900' Date1,'' Column1, POD.OutDate Date3, '' Column2, POD.OutDate Date4,POD.OutMiti Column8, 
                //                            '' Column3,'' Column4,'' Column5, '' Column6, SUM(POD.ChargedAmount) Decimal2, SUM(POD.CashAmount) Decimal1, '' Column7 
                //			                FROM ParkingOutDetails POD WHERE ( POD.OutDate BETWEEN {0} AND {1}) GROUP BY POD.OutDate, POD.OutMiti
                //                            UNION ALL
                //                            SELECT 'GT' Column9, '01/01/1900' Date1,'' Column1, '01/01/2200' Date3, '' Column2,'01/01/2200' Date4,'' Column8, 
                //                            '' Column3,'' Column4,'' Column5, '' Column6, SUM(POD.ChargedAmount) Decimal2, SUM(POD.CashAmount) Decimal1, '' Column7 
                //			                FROM ParkingOutDetails POD WHERE ( POD.OutDate BETWEEN {0} AND {1}) ) RS ORDER BY Date3, Column9, Date2,Date1,Column7,Column4";
                strSQL = string.Format(@"SELECT PID.InDate + CAST(PID.InTime AS datetime) Date1, PID.InMiti + ' ' + CAST(PID.InTime AS VARCHAR) Column1, POD.OutDate Date3,
                            PID.PlateNo Column2, POD.OutDate + CAST(POD.OutTime AS DATETIME) Date2, POD.OutMiti + ' ' + CAST(POD.OutTime AS VARCHAR) Column8,
                            POD.Interval Column3, VT.[Description] Column4, RM.RateDescription Column6, POD.ChargedAmount Decimal2,
                            POD.CashAmount Decimal1, U.FullName Column7 FROM ParkingInDetails PID 
                            INNER JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND PID.FYID = POD.FYID
                            INNER JOIN VehicleType VT ON VT.VTypeID = PID.VehicleType
                            INNER JOIN RateMaster RM ON RM.Rate_ID = POD.Rate_ID
                            INNER JOIN Users U ON U.[UID] = POD.[UID]                            
                            WHERE ( POD.OutDate BETWEEN '{0}' AND '{1}')
                            ORDER BY Date2,Date1,Column7,Column4", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));

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

        internal void Sort(DataGridSortingEventArgs e)
        {
            switch (e.Column.SortDirection)
            {
                case ListSortDirection.Ascending:
                    e.Column.SortDirection = ListSortDirection.Descending;
                    break;

                case ListSortDirection.Descending:
                    e.Column.SortDirection = ListSortDirection.Ascending;
                    break;

                default:
                    e.Column.SortDirection = ListSortDirection.Ascending;
                    break;
            }

            var column = e.Column.SortMemberPath;

        }
    }

   
  
}
