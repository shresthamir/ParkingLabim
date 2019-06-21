using Dapper;
using Newtonsoft.Json.Linq;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Data;
namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for wTranLogReport.xaml
    /// </summary>
    public partial class wTranLogReport : Window
    {
        public wTranLogReport()
        {
            InitializeComponent();
            this.DataContext = new vmTranLogReport(this);
        }
    }

    class vmTranLogReport : BaseViewModel
    {
        private byte _FLAG = 3;
        private DateTime _ToDate = DateTime.Today;
        private DateTime _FromDate = DateTime.Today;
        private string _TrnUser;
        private string _EntryForm;
        private string _Action;
        private string _ComputerName;


        private ObservableCollection<dynamic> _ActionList;
        private ObservableCollection<dynamic> _ComputerList;
        private ObservableCollection<dynamic> _FormList;
        private ObservableCollection<dynamic> _UserList;
        private ObservableCollection<dynamic> _ReportSource;

        public byte FLAG { get { return _FLAG; } set { _FLAG = value; OnPropertyChanged("FLAG"); } }
        public DateTime FromDate { get { return _FromDate; } set { _FromDate = value; OnPropertyChanged("FromDate"); } }
        public DateTime ToDate { get { return _ToDate; } set { _ToDate = value; OnPropertyChanged("ToDate"); } }
        public string TrnUser { get { return _TrnUser; } set { _TrnUser = value; OnPropertyChanged("TrnUser"); } }
        public string EntryForm { get { return _EntryForm; } set { _EntryForm = value; OnPropertyChanged("EntryForm"); } }
        public string ComputerName { get { return _ComputerName; } set { _ComputerName = value; OnPropertyChanged("ComputerName"); } }
        public string TrnAction { get { return _Action; } set { _Action = value; OnPropertyChanged("TrnAction"); } }

        public ObservableCollection<dynamic> UserList { get { return _UserList; } set { _UserList = value; OnPropertyChanged("UserList"); } }
        public ObservableCollection<dynamic> FormList { get { return _FormList; } set { _FormList = value; OnPropertyChanged("FormList"); } }
        public ObservableCollection<dynamic> ComputerList { get { return _ComputerList; } set { _ComputerList = value; OnPropertyChanged("ComputerList"); } }
        public ObservableCollection<dynamic> ActionList { get { return _ActionList; } set { _ActionList = value; OnPropertyChanged("ActionList"); } }
        public ObservableCollection<dynamic> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }
        ReportViewer rv;
        public vmTranLogReport(wTranLogReport w)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    UserList = new ObservableCollection<dynamic>(conn.Query("SELECT UserName FROM Users ORDER BY UserName"));
                    FormList = new ObservableCollection<dynamic>(conn.Query("Select distinct FormName from tblUserWorkingLog ORDER BY FormName"));
                    ComputerList = new ObservableCollection<dynamic>(conn.Query("Select distinct HostName from SESSION ORDER BY HostName"));
                    ActionList = new ObservableCollection<dynamic>(conn.Query("Select distinct TrnMode from tblUserWorkingLog ORDER BY TrnMode"));
                }
                PrintPreviewCommand = new RelayCommand(ExecutePrintPreview, CanExecutePrintExport);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrintExport);
                LoadData = new RelayCommand(ExecuteLoad);
                ExportCommand = new RelayCommand(ExecuteExport);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExecuteExport(object obj)
        {
            GlobalClass.ReportName = "Transaction Activity Log";
            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FromDate.ToString("MM/dd/yyyy"), ToDate.ToString("MM/dd/yyyy"));
            wExportFormat ef = new wExportFormat(rv.Report);
            ef.ShowDialog();
        }

        private void ExecutePrintPreview(object obj)
        {
            GlobalClass.ReportName = "Transaction Activity Log";
            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FromDate.ToString("MM/dd/yyyy"), ToDate.ToString("MM/dd/yyyy"));

            rv.Report.PrintSettings.PrintPageMargin = new Thickness(30);
            rv.Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;
            rv.Report.ShowPrintPreview();
        }
        private void ExecutePrint(object obj)
        {
            GlobalClass.ReportName = "Transaction Activity Log";
            GlobalClass.ReportParams = string.Format("From Date : {0} To {1}", FromDate.ToString("MM/dd/yyyy"), ToDate.ToString("MM/dd/yyyy"));

            rv.Report.PrintSettings.PrintPageMargin = new Thickness(30);
            rv.Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;
            rv.Report.Print();
        }

        private bool CanExecutePrintExport(object obj)
        {
            return ReportSource != null && ReportSource.Count > 0;
        }


        private void ExecuteLoad(object obj)
        {
            try
            {

                rv = new ReportViewer();
                string Param = string.Format("{{\"FLG\" : \"{0}\", \"SDATE\" : \"{1}\",  \"EDATE\" : \"{2}\"", FLAG, FromDate.ToString("MM/dd/yyyy"), ToDate.ToString("MM/dd/yyyy"));
                if (!string.IsNullOrEmpty(TrnUser))
                {
                    Param += string.Format(", \"USERID\" : \"{0}\"", TrnUser);
                }

                if (!string.IsNullOrEmpty(ComputerName))
                {
                    Param += string.Format(", \"HOSTNM\" : \"{0}\"", ComputerName);
                }

                if (!string.IsNullOrEmpty(EntryForm))
                {
                    Param += string.Format(", \"FORMNM\" : \"{0}\"", EntryForm);
                }

                if (!string.IsNullOrEmpty(TrnAction))
                {
                    Param += string.Format(", \"ACTIONNM\" : \"{0}\"", TrnAction);
                }

                Param += "}";


                // 

                LoadColumns();

                var data = GetDataFromProcedure("sp_UserWLogDetail", Param);
                if (data != null && data.Count() == 0)
                {
                    MessageBox.Show("NoData");
                }
                else
                {
                    ReportSource = new ObservableCollection<dynamic>(data);
                }

                GlobalClass.SetUserActivityLog("Transaction Activities Log", "View", string.Empty, string.Empty, string.Empty);
                rv.DataContext = this;
                rv.Show();


                //using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                //using (DataAccess da = new DataAccess())
                //{
                //    Conn.Open();
                //    //CommandDefinition Cmd = new CommandDefinition(ProcName,this,null,null,System.Data.CommandType.StoredProcedure);
                //    //Cmd.CommandText = ProcName;
                //    //Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                //    DynamicParameters d = new DynamicParameters();
                //    foreach (ReportDetail rd in Parameters)
                //    {
                //        if (string.IsNullOrEmpty(rd.PropName))
                //        {
                //            if (!string.IsNullOrEmpty(rd.Value))
                //                d.Add(rd.ParamName, rd.Value);
                //            continue;
                //        }
                //        if (CheckDefaultParameter(rd))
                //            continue;
                //        d.Add(rd.ParamName, GetValue(rd));
                //        //Cmd.Parameters.AddWithValue(rd.ParamName, GetValue(rd));
                //    }



                //    //ReportSource = new VirtualizingCollectionView(Conn.Query<TrnMain>(ProcName, d, commandType: System.Data.CommandType.StoredProcedure));
                //    ReportSource = new ObservableCollection<dynamic>(Conn.Query<dynamic>(ProcName, d, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: 600));

                //    LoadColumns(rv, Conn);

                //    foreach (dynamic cm in Conn.Query("SELECT MENUNAME, COMMANDNAME FROM REPORTCONTEXTMENU WHERE REPORTNAME ='" + ReportName + "'"))
                //    {
                //        MenuItem mi = new MenuItem();
                //        mi.Header = cm.MENUNAME;
                //        Binding b = new Binding(cm.COMMANDNAME);
                //        mi.SetBinding(MenuItem.CommandProperty, b);
                //        Binding CommandParameterBinding = new Binding("Data");
                //        CommandParameterBinding.Source = rv.FindResource("proxy");
                //        // mi.CommandParameter = SelectedItem;
                //        //CommandParameterBinding.Path = new System.Windows.PropertyPath("SelectedItem", null);
                //        //CommandParameterBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(SfDataGrid), 1);
                //        mi.SetBinding(MenuItem.CommandParameterProperty, CommandParameterBinding);
                //        rvContextMenu.Add(mi);
                //    }
                //}


                //rv.DataContext = this;
                //rv.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadColumns()
        {
            rv.Report.Columns.Clear();
            if (FLAG == 3)
            {
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "User Name", DisplayBinding = new Binding("UserId"), Width = 200 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "New Entry", DisplayBinding = new Binding("TOT_NEW"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Edit", DisplayBinding = new Binding("TOT_EDIT"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Delete", DisplayBinding = new Binding("TOT_DELETE"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Re-Print", DisplayBinding = new Binding("TOT_REPRINT"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "View", DisplayBinding = new Binding("TOT_VIEW"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Import/Export", DisplayBinding = new Binding("TOT_IMP_EXP"), Width = 150 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Backup/Restore", DisplayBinding = new Binding("TOT_BKP_RES"), Width = 150 });
            }
            else if (FLAG == 2)
            {
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "User Name", DisplayBinding = new Binding("UserId"), Width = 200 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Form Name", DisplayBinding = new Binding("FormName"), Width = 250 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "New Entry", DisplayBinding = new Binding("TOT_NEW"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Edit", DisplayBinding = new Binding("TOT_EDIT"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Delete", DisplayBinding = new Binding("TOT_DELETE"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Re-Print", DisplayBinding = new Binding("TOT_REPRINT"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "View", DisplayBinding = new Binding("TOT_VIEW"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Import/Export", DisplayBinding = new Binding("TOT_IMP_EXP"), Width = 150 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Backup/Restore", DisplayBinding = new Binding("TOT_BKP_RES"), Width = 150 });
            }
            else if (FLAG == 1)
            {
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "User Name", DisplayBinding = new Binding("UserId"), Width = 150 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Computer Name", DisplayBinding = new Binding("HOSTNAME"), Width = 150 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Form Name", DisplayBinding = new Binding("FormName"), Width = 200 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Date", DisplayBinding = new Binding("TrnDate"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Time", DisplayBinding = new Binding("TrnTime"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Action", DisplayBinding = new Binding("TrnMode"), Width = 100 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Work Detail", DisplayBinding = new Binding("WorkDetail"), Width = 170 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Voucher No", DisplayBinding = new Binding("VchrNo"), Width = 120 });
                rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Remarks", DisplayBinding = new Binding("Remarks"), Width = 150 });
            }
        }


        public static IEnumerable<dynamic> GetDataFromProcedure(string ProcName, string ParamJSON)
        {
            try
            {
                JObject paramter = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(ParamJSON);
                DynamicParameters d = new DynamicParameters();
                foreach (KeyValuePair<string, JToken> pi in paramter)
                {
                    d.Add("@" + pi.Key, pi.Value.ToString());
                }
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    return conn.Query<dynamic>(ProcName, d, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }

    class vmFootFallReport : BaseViewModel
    {
        ReportViewer rv;
        private ObservableCollection<dynamic> _ReportSource;

        public ObservableCollection<dynamic> ReportSource { get { return _ReportSource; } set { _ReportSource = value; OnPropertyChanged("ReportSource"); } }

        public vmFootFallReport()
        {
            PrintPreviewCommand = new RelayCommand(ExecutePrintPreview, CanExecutePrintExport);
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrintExport);
            LoadData = new RelayCommand(ExecuteLoad);
        }

        private void ExecuteExport(object obj)
        {
            GlobalClass.ReportName = "Footfall Report";
            GlobalClass.ReportParams = "";
            wExportFormat ef = new wExportFormat(rv.Report);
            ef.ShowDialog();
        }

        private void ExecutePrintPreview(object obj)
        {
            GlobalClass.ReportName = "Footfall Report";
            GlobalClass.ReportParams = "";

            rv.Report.PrintSettings.PrintPageMargin = new Thickness(30);
            rv.Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;
            rv.Report.ShowPrintPreview();
        }
        private void ExecutePrint(object obj)
        {
            GlobalClass.ReportName = "Footfall Report";
            GlobalClass.ReportParams = "";

            rv.Report.PrintSettings.PrintPageMargin = new Thickness(30);
            rv.Report.PrintSettings.AllowColumnWidthFitToPrintPage = false;
            rv.Report.Print();
        }

        private bool CanExecutePrintExport(object obj)
        {
            return ReportSource != null && ReportSource.Count > 0;
        }

        private void ExecuteLoad(object obj)
        {
            try
            {
                rv = new ReportViewer();
                LoadColumns();

                var data = GetDataFromProcedure("sp_FootfallReport", "");
                if (data != null && data.Count() == 0)
                {
                    MessageBox.Show("NoData");
                }
                else
                {
                    ReportSource = new ObservableCollection<dynamic>(data);
                }

                GlobalClass.SetUserActivityLog("Footfall Report", "View", string.Empty, string.Empty, string.Empty);
                rv.DataContext = this;
                rv.Show();
            }

            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void LoadColumns()
        {
            rv.Report.Columns.Clear();
            rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Month", DisplayBinding = new Binding("Month"), Width = 200 });
            rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Two Wheeler", DisplayBinding = new Binding("Two Wheeler"), Width = 120 });
            rv.Report.Columns.Add(new GridTextColumn { HeaderText = "Four Wheeler", DisplayBinding = new Binding("Four Wheeler"), Width = 120 });
        }


        public static IEnumerable<dynamic> GetDataFromProcedure(string ProcName, string ParamJSON)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    return conn.Query<dynamic>(ProcName, commandType: CommandType.StoredProcedure);
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
