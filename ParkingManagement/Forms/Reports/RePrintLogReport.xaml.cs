
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
namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for Report_Dialog.xaml
    /// </summary>
    public partial class RePrintLogReport : UserControl
    {


        public RePrintLogReport(byte Flag = 0)
        {
            InitializeComponent();
            this.chkVatSalesRegister.Visibility = (Flag == 4) ? Visibility.Visible : Visibility.Collapsed;
            this.lbSummary.Visibility = this.cmbSummary.Visibility = (Flag == 8) ? Visibility.Visible : Visibility.Collapsed;
            this.DataContext = new vmPrintLogReport() { Report = Report, ReportFlag = Flag };
        }
    }


    class vmPrintLogReport : BaseViewModel
    {
        public Syncfusion.UI.Xaml.Grid.SfDataGrid Report { get; set; }
        private ObservableCollection<ReportModel> _ReportSource;
        private DateTime _TDate;
        private DateTime _FDate;
        private bool _ShowSISummary;
        private string _summaryType = "REMARKS";
        public bool ShowSISummary { get { return _ShowSISummary; } set { _ShowSISummary = value; OnPropertyChanged("ShowSISummary"); } }
        public byte ReportFlag { get; set; } // 0:Reprint Log, 1: ANNEX7, 2: ABB SALES REGISTER SUMMARY
        public ObservableCollection<ReportModel> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }
        public DateTime TDate { get { return _TDate; } set { _TDate = value; OnPropertyChanged("TDate"); } }
        public DateTime FDate { get { return _FDate; } set { _FDate = value; OnPropertyChanged("FDate"); } }
        public string SummaryType { get { return _summaryType; } set { _summaryType = value; OnPropertyChanged("SummaryType"); } }
        public vmPrintLogReport()
        {
            FDate = DateTime.Today.Date;
            TDate = DateTime.Today.Date;
            PrintPreviewCommand = new RelayCommand(ExecutePrintPreview, CanExecutePrintExport);
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrintExport);
            LoadData = new RelayCommand(LoadReport);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecutePrintExport);
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
            Report.PrintSettings.PrintPageOrientation = PrintOrientation.Landscape;
            Report.Print();
        }

        private void LoadReport(object param)
        {
            string strSql = string.Empty;
            try
            {
                if (ReportFlag == 0)
                    strSql = string.Format(@"SELECT F.FYNAME FISCAL_YEAR,  BillNo BILL_NO,PrintDate PRINTED_DATE,PrintTIME PRINTED_TIME,PrintUser PRINTED_BY,ROW_NUMBER() OVER (PARTITION BY BILLNO,PL.FYID ORDER BY PrintDATE,CONVERT(DATETIME,PrintTIME)) REPRINTNO 
                                    FROM tblPrintLog PL JOIN tblFiscalYear F ON PL.FYID = F.FYID
                                    WHERE PrintDATE BETWEEN '{0}' and '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                else if (ReportFlag == 1)
                    strSql = string.Format("SELECT * FROM VIEW_ANNEX7 WHERE BILL_DATE BETWEEN '{0}' AND '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                else if (ReportFlag == 2)
                    strSql = string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, MITI,'SI' + CONVERT(VARCHAR,MIN(BillNo)) + ' - ' + 'SI' + CONVERT(VARCHAR,MAX(BillNo)) BILL_NO,'Abb. Tax Invoice'  CUSTOMER_NAME, NULL CUSTOMER_PAN,
                                SUM(NETSALE) NETSALE,SUM(NonTaxable) NONTAXABLE, NULL ZERORATED, SUM(Taxable) TAXABLE_AMOUNT,
                                SUM(VAT) TAX_AMOUNT FROM
                                (SELECT  TDATE,TMITI Miti,CONVERT(Numeric,SUBSTRING(BillNo,3,LEN(BillNo))) BillNo,
                                Amount,Discount,ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE ,Taxable,
                                NonTaxable,VAT,GrossAmount NetAmount 
                                FROM ParkingSales TM 
								WHERE LEFT(BillNo,2) = 'SI' AND TDATE BETWEEN '{0}' AND '{1}'
								) tbl GROUP BY TDATE,Miti ORDER BY TDATE", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                else if (ReportFlag == 3)
                    strSql = string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, TMITI Miti,BillNo BILL_NO,ISNULL(BillTo,'Cash Sales') CUSTOMER_NAME, BILLTOPAN CUSTOMER_PAN,
                                ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE, ISNULL(NonTaxable,0) NONTAXABLE, NULL ZERORATED, Taxable TAXABLE_AMOUNT,
                                VAT TAX_AMOUNT, TDate BILL_TIME, CAST(SUBSTRING(BILLNO,3,LEN(BILLNO)) AS INT) SNO 
                                FROM ParkingSales Where LEFT(BillNo,2) = 'SI' AND TDate BETWEEN '{0}' AND '{1}' AND BillNo LIKE 'SI%' ORDER BY TDate,SNO", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                //VAT SALES REGISTER
                else if (ReportFlag == 4)
                {
                    strSql = string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, TMiti Miti,BillNo BILL_NO,ISNULL(BillTo,'Cash Sales') CUSTOMER_NAME, BillToPan CUSTOMER_PAN,
                                ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE, ISNULL(NonTaxable,0) NONTAXABLE, 0 ZERORATED, Taxable TAXABLE_AMOUNT,
                                VAT TAX_AMOUNT, TTIME BILL_TIME, DISCOUNT, CAST(SUBSTRING(BILLNO,3,LEN(BILLNO)) AS INT) SNO 
                                FROM ParkingSales Where TDATE BETWEEN '{0}' AND '{1}' AND BillNo LIKE 'TI%'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                    strSql += Environment.NewLine + "UNION ALL";
                    if (ShowSISummary)
                        strSql += Environment.NewLine + string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, MITI,'SI' + CONVERT(VARCHAR,MIN(BillNo)) + ' - ' + 'SI' + CONVERT(VARCHAR,MAX(BillNo)) BILL_NO,'Abb. Tax Invoice'  CUSTOMER_NAME, NULL CUSTOMER_PAN,
                                SUM(NETSALE) NETSALE,SUM(NonTaxable) NONTAXABLE, 0 ZERORATED, SUM(Taxable) TAXABLE_AMOUNT,
                                SUM(VAT) TAX_AMOUNT, '' BILL_TIME, SUM(DISCOUNT) DISCOUNT, 0 SNO FROM
                                (SELECT  TDATE,TMITI Miti,CONVERT(Numeric,SUBSTRING(BillNo,3,LEN(BillNo))) BillNo,
                                Amount,Discount,ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE ,Taxable,
                                NonTaxable,VAT,GrossAmount NetAmount
                                FROM ParkingSales TM 
								WHERE LEFT(BillNo,2) = 'SI' AND TDATE BETWEEN '{0}' AND '{1}'
								) tbl GROUP BY TDATE,Miti ORDER BY DATE, SNO", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                    else
                        strSql += Environment.NewLine + string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, TMITI Miti,BillNo BILL_NO,ISNULL(BillTo,'Cash Sales') CUSTOMER_NAME, BILLTOPAN CUSTOMER_PAN,
                                ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE, ISNULL(NonTaxable,0) NONTAXABLE, 0 ZERORATED, Taxable TAXABLE_AMOUNT,
                                VAT TAX_AMOUNT, TDate BILL_TIME, DISCOUNT, CAST(SUBSTRING(BILLNO,3,LEN(BILLNO)) AS INT) SNO 
                                FROM ParkingSales Where LEFT(BillNo,2) = 'SI' AND TDate BETWEEN '{0}' AND '{1}' AND BillNo LIKE 'SI%' ORDER BY Date,SNO", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                }
                else if (ReportFlag == 5)
                    strSql = string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, TMiti Miti,BillNo BILL_NO, RefBillNo REF_NO, ISNULL(BillTo,'Cash Sales') CUSTOMER_NAME, BILLTOPAN CUSTOMER_PAN,
                                ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETSALE, ISNULL(NonTaxable,0) NONTAXABLE, NULL ZERORATED, Taxable TAXABLE_AMOUNT,
                                VAT TAX_AMOUNT, TTIME BILL_TIME, CAST(SUBSTRING(BILLNO,3,LEN(BILLNO)) AS INT) SNO, REFBILLNO, REMARKS
                                FROM ParkingSales Where TDATE BETWEEN '{0}' AND '{1}' AND BillNo LIKE 'CN%' ORDER BY TDATE,SNO", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                else if (ReportFlag == 6)
                    strSql = string.Format(@"SELECT CONVERT(VARCHAR,TDATE,101) DATE, TMiti Miti,BillNo BILL_NO,ISNULL(BillToAdd,'Cash Purchase') SUPPLIER_NAME, BILLTOPAN SUPPLIER_PAN,
                                ISNULL(Taxable,0) + ISNULL(NonTaxable,0) NETPURCHASE, ISNULL(NonTaxable,0) NONTAXABLE, NULL ZERORATED, Taxable TAXABLE_AMOUNT,
                                VAT TAX_AMOUNT, 0 TAXABLE_IMPORT, 0 TAX_IMPORT,0 TAXABLE_CAPITAL,0 TAX_CAPITAL, TTIME BILL_TIME, CAST(SUBSTRING(BILLNO,3,LEN(BILLNO)) AS INT) SNO 
                                FROM ParkingSales Where TDATE BETWEEN '{0}' AND '{1}' AND BillNo LIKE 'PI%' ORDER BY TDATE,SNO", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                //VOUCHER DISCOUNT REPORT - DETAILS
                else if (ReportFlag == 7)
                    strSql = string.Format(@"SELECT VSD.BillNo BILL_NO, CAST(CONVERT(VARCHAR(10),PV.ScannedTime, 101) AS DATETIME) BILL_DATE, RIGHT( CONVERT(VARCHAR, ScannedTime, 0),7) PRINTED_TIME, VSD.VoucherNo REF_NO, PV.VoucherName REMARKS, VSD.DiscountAmount DISCOUNT, PSV.BillTo CUSTOMER_NAME, U.UserName PRINTED_BY 
                                            FROM VoucherDiscountDetail VSD 
                                            JOIN ParkingVouchers PV ON VSD.VoucherNo = PV.VoucherNo --AND PV.FYID = VSD.FYID
                                            LEFT JOIN ParkingSales PSV ON PV.BillNo = PSV.BillNo AND PSV.FYID = PV.FYID 
                                            JOIN USERS U ON U.UID = VSD.UID WHERE ScannedTime BETWEEN '{0}' AND '{1}'", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy") + " 23:59:59");
                //VOUCHER DISCOUNT REPORT - SUMMARY
                else if (ReportFlag == 8)
                {

                    strSql = string.Format(@"SELECT BILL_DATE, {0}, COUNT(*) REPRINTNO, SUM(DISCOUNT) DISCOUNT FROM
                                                (
                                                SELECT CAST(CONVERT(VARCHAR(10),PV.ScannedTime, 101) AS DATETIME) BILL_DATE, PV.VoucherName REMARKS, VSD.DiscountAmount DISCOUNT, PSV.BillTo CUSTOMER_NAME, U.UserName PRINTED_BY 
                                                FROM VoucherDiscountDetail VSD 
                                                JOIN ParkingVouchers PV ON VSD.VoucherNo = PV.VoucherNo -- AND PV.FYID = VSD.FYID
                                                LEFT JOIN ParkingSales PSV ON PV.BillNo = PSV.BillNo AND PSV.FYID = PV.FYID 
                                                JOIN USERS U ON U.UID = VSD.UID WHERE ScannedTime BETWEEN '{1}' AND '{2}'
                                                ) a GROUP BY BILL_DATE, {0} ORDER BY BILL_DATE", SummaryType, FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy") + " 23:59:59");
                }
                else if (ReportFlag == 9)
                {
                    strSql = string.Format(@"SELECT VT.[Description] CUSTOMER_NAME, InDate PRINTED_DATE, InMiti DATE, InTime PRINTED_TIME, OutDate BILL_DATE, OutMiti MITI, OutTime CUSTOMER_PAN, Interval REMARKS, ChargedAmount TAXABLE_AMOUNT, ChargedHours TAXABLE_IMPORT 
                                                FROM ParkingInDetails PID JOIN VehicleType VT ON Vt.VTypeID = PID.VehicleType
                                                JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND PID.FYID = POD.FYID
                                                WHERE STAFF_BARCODE = 'STAMP' AND OutDate BETWEEN '{0}' AND '{1}' 
                                                ORDER BY OutDate, OutTime", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                }
                else if (ReportFlag == 10)
                {
                    strSql = string.Format(@"SELECT POD.OutDate BILL_DATE, POD.OutMiti MITI, VT.[Description] CUSTOMER_NAME, COUNT(POD.PID) REPRINTNO, SUM(ChargedAmount) TAXABLE_AMOUNT, SUM(ChargedHours) TAXABLE_IMPORT
                                                FROM ParkingInDetails PID JOIN VehicleType VT ON Vt.VTypeID = PID.VehicleType
                                                JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND PID.FYID = POD.FYID
                                                WHERE STAFF_BARCODE = 'STAMP' AND OutDate BETWEEN '{0}' AND '{1}'
                                                GROUP BY VT.[Description], POD.OutDate, POD.OutMiti
                                                ORDER BY POD.OutDate, VT.[Description]", FDate.ToString("MM/dd/yyyy"), TDate.ToString("MM/dd/yyyy"));
                }


                LoadColumns();
                var data = GetDataTable(strSql);
                if (data != null && data.Count() == 0)
                    MessageBox.Show("NoData");
                else
                    ReportSource = new ObservableCollection<ReportModel>(data);
                GlobalClass.SetUserActivityLog(GetReportName(), "View", string.Empty, string.Empty, string.Empty);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void LoadColumns()
        {

            Report.Columns.Clear();
            Report.TableSummaryRows.Clear();
            Report.StackedHeaderRows.Clear();
            if (ReportFlag == 0)
            {

                Report.Columns.Add(new GridTextColumn { HeaderText = "Fiscal Year", DisplayBinding = new Binding("FISCAL_YEAR"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Bill No", DisplayBinding = new Binding("BILL_NO"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Printed Date", DisplayBinding = new Binding("PRINTED_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Printed Time", DisplayBinding = new Binding("PRINTED_TIME"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Printed By", DisplayBinding = new Binding("PRINTED_BY"), Width = 150 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Reprint No", DisplayBinding = new Binding("REPRINTNO"), Width = 100 });
            }
            else if (ReportFlag == 1)
            {

                Report.Columns.Add(new GridTextColumn { HeaderText = "FISCAL_YEAR", DisplayBinding = new Binding("FISCAL_YEAR"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "BILL_NO", DisplayBinding = new Binding("BILL_NO"), Width = 80 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER_NAME", DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 150 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER_PAN", DisplayBinding = new Binding("CUSTOMER_PAN"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "BILL_DATE", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("AMOUNT") { StringFormat = "#0.00" }, Width = 90, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "DISCOUNT", DisplayBinding = new Binding("DISCOUNT") { StringFormat = "#0.00" }, Width = 90, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAXABLE_AMOUNT", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#0.00" }, Width = 120, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAX_AMOUNT", DisplayBinding = new Binding("TAX_AMOUNT") { StringFormat = "#0.00" }, Width = 100, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TOTAL_AMOUNT", DisplayBinding = new Binding("TOTAL_AMOUNT") { StringFormat = "#0.00" }, Width = 100, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "SYNC_WITH_IRD", DisplayBinding = new Binding("SYNC_WITH_IRD"), Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "IS_BILL_PRINTED", DisplayBinding = new Binding("IS_PRINTED"), Width = 90 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "IS_BILL_ACTIVE", DisplayBinding = new Binding("IS_ACTIVE"), Width = 80 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "PRINTED_TIME", DisplayBinding = new Binding("PRINTED_TIME"), Width = 110 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "ENTERED_BY", DisplayBinding = new Binding("ENTERED_BY"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "PRINTED_BY", DisplayBinding = new Binding("PRINTED_BY"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "IS_REALTIME", DisplayBinding = new Binding("IS_REAL_TIME"), Width = 100 });
            }

            else if (ReportFlag == 2 || ReportFlag == 3 || ReportFlag == 4)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = "DATE", DisplayBinding = new Binding("DATE"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "MITI", DisplayBinding = new Binding("MITI"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "INVOICE NO", DisplayBinding = new Binding("BILL_NO"), Width = 110 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER NAME", DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 130 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER PAN", DisplayBinding = new Binding("CUSTOMER_PAN"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TOTAL SALES", DisplayBinding = new Binding("NETSALE") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "NON TAXABLE SALES", DisplayBinding = new Binding("NONTAXABLE") { StringFormat = "#0.00" }, Width = 145, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "EXPORT SALES", DisplayBinding = new Binding("ZERORATED") { StringFormat = "#0.00" }, Width = 135, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "DISCOUNT", DisplayBinding = new Binding("DISCOUNT") { StringFormat = "#0.00" }, Width = 135, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAX", DisplayBinding = new Binding("TAX_AMOUNT") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });

                StackedHeaderRow shr = new StackedHeaderRow();
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "INVOICE", ChildColumns = "DATE,MITI,BILL_NO,CUSTOMER_NAME,CUSTOMER_PAN" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TAXABLE SALES", ChildColumns = "TAXABLE_AMOUNT,TAX_AMOUNT" });
                Report.StackedHeaderRows.Add(shr);

                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NETSALE", Name = "TNETSALE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NONTAXABLE", Name = "TNONTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "ZERORATED", Name = "TZERORATED", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_AMOUNT", Name = "TVAT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }

            else if (ReportFlag == 5)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = "DATE", DisplayBinding = new Binding("DATE"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "MITI", DisplayBinding = new Binding("MITI"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "C.N. NO", DisplayBinding = new Binding("BILL_NO"), Width = 80 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "REF NO", DisplayBinding = new Binding("REF_NO"), Width = 80 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER NAME", DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 130 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "CUSTOMER PAN", DisplayBinding = new Binding("CUSTOMER_PAN"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("NETSALE") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("NONTAXABLE") { StringFormat = "#0.00" }, Width = 100, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("ZERORATED") { StringFormat = "#0.00" }, Width = 90, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "VAT", DisplayBinding = new Binding("TAX_AMOUNT") { StringFormat = "#0.00" }, Width = 70, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "REMARKS", DisplayBinding = new Binding("REMARKS"), Width = 150 });

                StackedHeaderRow shr = new StackedHeaderRow();
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "INVOICE", ChildColumns = "DATE,MITI,BILL_NO,REF_NO,CUSTOMER_NAME,CUSTOMER_PAN" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TOTAL", ChildColumns = "NETSALE" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "NON TAXABLE", ChildColumns = "NONTAXABLE" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "ZERO RATED", ChildColumns = "ZERORATED" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TAXABLE", ChildColumns = "TAXABLE_AMOUNT,TAX_AMOUNT" });
                Report.StackedHeaderRows.Add(shr);

                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NETSALE", Name = "TNETSALE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NONTAXABLE", Name = "TNONTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "ZERORATED", Name = "TZERORATED", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_AMOUNT", Name = "TVAT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
            else if (ReportFlag == 6)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = "DATE", DisplayBinding = new Binding("DATE"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "MITI", DisplayBinding = new Binding("MITI"), Width = 75 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "INVOICE NO", DisplayBinding = new Binding("BILL_NO"), Width = 110 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "SUPPLIER NAME", DisplayBinding = new Binding("SUPPLIER_NAME"), Width = 130 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "SUPPLIER VAT NO", DisplayBinding = new Binding("SUPPLIER_PAN"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "PURCHASE VALUE", DisplayBinding = new Binding("NETPURCHASE") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("NONTAXABLE") { StringFormat = "#0.00" }, Width = 145, TextAlignment = TextAlignment.Right });

                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAX", DisplayBinding = new Binding("TAX_AMOUNT") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });

                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("TAXABLE_IMPORT") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAX", DisplayBinding = new Binding("TAX_IMPORT") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });

                Report.Columns.Add(new GridTextColumn { HeaderText = "AMOUNT", DisplayBinding = new Binding("TAXABLE_CAPITAL") { StringFormat = "#0.00" }, Width = 95, TextAlignment = TextAlignment.Right });
                Report.Columns.Add(new GridTextColumn { HeaderText = "TAX", DisplayBinding = new Binding("TAX_CAPITAL") { StringFormat = "#0.00" }, Width = 80, TextAlignment = TextAlignment.Right });

                StackedHeaderRow shr = new StackedHeaderRow();
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "INVOICE", ChildColumns = "DATE,MITI,BILL_NO,CUSTOMER_NAME,CUSTOMER_PAN" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TOTAL", ChildColumns = "NETPURCHASE" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "NON TAXABLE", ChildColumns = "NONTAXABLE" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TAXABLE PURCHASE", ChildColumns = "TAXABLE_AMOUNT,TAX_AMOUNT" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "TAXABLE IMPORT", ChildColumns = "TAXABLE_IMPORT,TAX_IMPORT" });
                shr.StackedColumns.Add(new StackedColumn() { HeaderText = "CAPITAL TAXABLE PURCHASE/IMPORT", ChildColumns = "TAXABLE_CAPITAL,TAX_IMPORT" });
                Report.StackedHeaderRows.Add(shr);

                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NETPURCHASE", Name = "TNETPURCHASE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "NONTAXABLE", Name = "TNONTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });

                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TTAXABLE", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_AMOUNT", Name = "TVAT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });

                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_IMPORT", Name = "TTAXABLEIMPORT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_IMPORT", Name = "TVATIMPORT", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });

                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_CAPITAL", Name = "TTAXABLECAPITAL", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAX_CAPITAL", Name = "TVATCAPITAL", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
            else if (ReportFlag == 7)
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
            else if (ReportFlag == 8)
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
            else if (ReportFlag == 9)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = LabelCaption.LabelCaptions["Vehicle Type"], DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "In Date", DisplayBinding = new Binding("PRINTED_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "In Time", DisplayBinding = new Binding("PRINTED_TIME"), Width = 70, TextAlignment = TextAlignment.Center });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Out Date", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Out Time", DisplayBinding = new Binding("CUSTOMER_PAN"), Width = 70, TextAlignment = TextAlignment.Center });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Interval", DisplayBinding = new Binding("REMARKS"), Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "C. Hours", DisplayBinding = new Binding("TAXABLE_IMPORT") { StringFormat = "#,###,#0.00" }, TextAlignment = TextAlignment.Right, Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "C. Amount", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#,###,#0.00" }, TextAlignment = TextAlignment.Right, Width = 120 });

                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "REMARKS", Name = "TInterval", SummaryType = Syncfusion.Data.SummaryType.CountAggregate, Format = "{Count}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_IMPORT", Name = "TCHours", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TCAmount", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
            else if (ReportFlag == 10)
            {
                Report.Columns.Add(new GridTextColumn { HeaderText = "Date", DisplayBinding = new Binding("BILL_DATE") { StringFormat = "MM/dd/yyyy" }, Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Miti", DisplayBinding = new Binding("MITI"), Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = LabelCaption.LabelCaptions["Vehicle Type"], DisplayBinding = new Binding("CUSTOMER_NAME"), Width = 120 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "Count", DisplayBinding = new Binding("REPRINTNO"), Width = 70 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "C. Hours", DisplayBinding = new Binding("TAXABLE_IMPORT") { StringFormat = "#,###,#0.00" }, TextAlignment = TextAlignment.Right, Width = 100 });
                Report.Columns.Add(new GridTextColumn { HeaderText = "C. Amount", DisplayBinding = new Binding("TAXABLE_AMOUNT") { StringFormat = "#,###,#0.00" }, TextAlignment = TextAlignment.Right, Width = 120 });
                GridSummaryRow Tgsr = new GridSummaryRow() { ShowSummaryInRow = false };
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "REPRINTNO", Name = "TCount", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_IMPORT", Name = "TCHours", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Tgsr.SummaryColumns.Add(new GridSummaryColumn() { MappingName = "TAXABLE_AMOUNT", Name = "TCAmount", SummaryType = Syncfusion.Data.SummaryType.DoubleAggregate, Format = "{Sum:#,###,#0.00}" });
                Report.TableSummaryRows.Add(Tgsr);
            }
        }

        string GetReportName()
        {
            if (ReportFlag == 0)
                return "Reprint Log Report";
            else if (ReportFlag == 1)
                return "ANNEX 7 Report";
            else if (ReportFlag == 2)
                return "Abbreviated Sales Register Report - Summary";
            else if (ReportFlag == 3)
                return "Abbreviated Sales Register Report - Details";
            else if (ReportFlag == 4)
                return "Vat Sales Register Report";
            else if (ReportFlag == 5)
                return "Credit Note Register Report";
            else if (ReportFlag == 6)
                return "Purchase Register Report";
            else if (ReportFlag == 7)
                return "Voucher Redeem Report - Details";
            else if (ReportFlag == 8)
                return "Voucher Redeem Report - Summary";
            else if (ReportFlag == 9)
                return "Parking Pass Report - Details";
            else if (ReportFlag == 10)
                return "Parking Pass Report - Summary";
            return "Reprint Log Report";
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

    struct ReportModel
    {
        public string DATE { get; set; }
        public string FISCAL_YEAR { get; set; }
        public string BILL_NO { get; set; }
        public DateTime PRINTED_DATE { get; set; }
        public string PRINTED_TIME { get; set; }
        public string PRINTED_BY { get; set; }
        public int REPRINTNO { get; set; }
        public string CUSTOMER_NAME { get; set; }
        public string CUSTOMER_PAN { get; set; }
        public DateTime BILL_DATE { get; set; }
        public decimal AMOUNT { get; set; }
        public decimal TOTAL_AMOUNT { get; set; }
        public decimal DISCOUNT { get; set; }
        public decimal TAXABLE_AMOUNT { get; set; }
        public decimal TAX_AMOUNT { get; set; }
        public decimal TAXABLE_IMPORT { get; set; }
        public decimal TAX_IMPORT { get; set; }
        public decimal TAXABLE_CAPITAL { get; set; }
        public decimal TAX_CAPITAL { get; set; }
        public byte IS_PRINTED { get; set; }
        public byte IS_ACTIVE { get; set; }
        public string MITI { get; set; }
        public decimal NETSALE { get; set; }
        public decimal? ZERORATED { get; set; }
        public decimal? NONTAXABLE { get; set; }
        public string REMARKS { get; set; }
        public string REF_NO { get; set; }
        public byte SYNC_WITH_IRD { get; set; }
        public byte IS_REAL_TIME { get; set; }
        public byte IS_BILL_PRINTED { get { return IS_PRINTED; } set { IS_PRINTED = value; } }
        public byte IS_BILL_ACTIVE { get { return IS_ACTIVE; } set { IS_ACTIVE = value; } }
    }

}


