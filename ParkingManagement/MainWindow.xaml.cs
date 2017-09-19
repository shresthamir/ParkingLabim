using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
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
using Xceed.Wpf.AvalonDock.Layout;
using Dapper;
using ParkingManagement.Forms.DataUtility;
namespace ParkingManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        IEnumerable<TMenu> _MenuList;
        private bool IsAppRunningInServer;
        public MainWindow()
        {

            InitializeComponent();
            Loaded += MainWindow_Loaded;

            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT SERVERPROPERTY('MachineName')";
                        IsAppRunningInServer = (Environment.MachineName == cmd.ExecuteScalar().ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                IsAppRunningInServer = false;
            }

            Assembly entryAssembly = Assembly.GetEntryAssembly();
            object[] customAttributes = entryAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            this.Closing += MainWindow_Closing;
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Do you want to close the application. Any unsaved activities would be discarded?", "Application Closing", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
            else
                GlobalClass.EndSession();
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var activeChild = LayDocPane.Children.FirstOrDefault(x => x.IsSelected);
                if (activeChild == null)
                    return;
                var uc = ((UserControl)activeChild.Content).DataContext;
                var dc = ((BaseViewModel)uc);
                switch (e.Key)
                {
                    case Key.N:
                        if (dc._action == BaseViewModel.ButtonAction.Init)
                            dc.NewCommand.Execute(null);
                        break;
                    case Key.E:
                        if (dc._action == BaseViewModel.ButtonAction.Init || dc._action == BaseViewModel.ButtonAction.Selected)
                            dc.EditCommand.Execute(null);
                        break;
                    case Key.S:
                        if (dc._action == BaseViewModel.ButtonAction.New || dc._action == BaseViewModel.ButtonAction.Edit)
                            dc.SaveCommand.Execute(null);
                        break;
                    case Key.D:
                        if (dc._action == BaseViewModel.ButtonAction.Selected)
                            dc.DeleteCommand.Execute(null);
                        break;
                    case Key.Z:
                        dc.UndoCommand.Execute(null);
                        break;
                }
            }
            //else
            //{
            //    LayoutAnchorable la = new LayoutAnchorable();
            //    switch(e.Key)
            //    {
            //        case Key.F1:
            //            if (GlobalClass.User.VehicleType)
            //            {
            //                la.Content = new Forms.Master.Vehicle_Type.ucVehicleType();
            //                la.Title = "Vehicle Type";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F2:
            //            if (GlobalClass.User.RateMaster)
            //            {
            //                //la.Content = new Forms.Reports.Cash_Collection_Reports.CCVehicleWise();
            //                la.Content = new Forms.Master.Rate.ucRate();
            //                la.Title = "Rate Setting";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F3:
            //            if (GlobalClass.User.PartySetting)
            //            {
            //                la.Content = new Forms.Master.Party_Setting.ucParty();
            //                la.Title = "Party Setting";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F4:
            //            if (GlobalClass.User.UserSetting)
            //            {
            //                la.Content = new Forms.Master.User_Setting.UserSetting();
            //                la.Title = "User Setting";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F5:
            //            if (GlobalClass.User.ParkingIn)
            //            {
            //                la.Content = new Forms.Transaction.Parking_In.ucParkingIn();
            //                la.Title = "Parking In";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F6:
            //            if (GlobalClass.User.ParkingOut)
            //            {
            //                la.Content = new Forms.Transaction.Parking_Out.ucParkingOut();
            //                la.Title = "Parking Out";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F7: if (GlobalClass.User.CashReceipt)
            //            {
            //                la.Content = new Forms.Transaction.Cash_Receipt.CashReceipt();
            //                la.Title = "Cash Receipt";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //        case Key.F8: if (GlobalClass.User.Reports)
            //            {
            //                la.Content = new Forms.Reports.ucDailySalesReport();
            //                la.Title = "Report";
            //                la.IsActive = true;
            //                LayDocPane.Children.Add(la);
            //            }
            //            break;
            //    }
            //}
        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    _MenuList = conn.Query<TMenu>("SELECT M.MID, MENUNAME, PARENT, FORMPATH, IMAGEPATH FROM MENU M JOIN UserRight UR ON M.MID = UR.MID WHERE UR.UID = '" + GlobalClass.User.UID + "' AND [OPEN] = 1");
                }
                foreach (TMenu child in _MenuList.Where(x => x.PARENT == 0))
                {
                    MenuItem mi = new MenuItem();
                    mi.Tag = child;
                    mi.Header = child.MENUNAME;
                    if (!string.IsNullOrEmpty(child.IMAGEPATH))
                    {
                        Image icon = new Image();
                        icon.Source = Imaging.FileToImage(child.IMAGEPATH);
                        mi.Icon = icon;
                    }
                    mi.Click += mi_Click;
                    LoadMenu(mi, child.MID);
                    MainMenu.Items.Add(mi);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Menu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        void mi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem mi = sender as MenuItem;
                TMenu m = (TMenu)mi.Tag;
                if (string.IsNullOrEmpty(m.FORMPATH))
                {
                    switch (m.MENUNAME)
                    {
                        case "LOG OUT":
                            LogOut();
                            break;
                        case "EXIT":
                            Exit();
                            break;
                        case "HELP":
                            new HelpViewer().Show();
                            break;
                        case "GENERATE COMPLEMENTARY PASS":
                            new Forms.GenerateComplementaryVouchers().ShowDialog();
                            break;
                        case "DATA BACKUP":
                            if (IsAppRunningInServer)
                            {
                                wDataBackup w = new wDataBackup();
                                w.ShowDialog();
                                return;
                            }
                            break;
                        case "DATA RECOVERY":
                            if (IsAppRunningInServer)
                            {
                                new wDataRestore().ShowDialog();
                            }
                            break;
                        case "PRINT DAY SUMMARY":
                            PrintDaySummary();
                            break;
                        default:
                            Reports(m.MENUNAME);
                            break;
                    }
                    return;
                }
                object content = GetUserControl(m.FORMPATH);
                if (content == null)
                {

                    //GenerateReportUI(m.FORMPATH);
                    return;
                }
                else if (content.GetType().BaseType == typeof(UserControl))
                {
                    if (!string.IsNullOrEmpty(m.DATACONTEXT))
                    {
                        ((FrameworkElement)content).DataContext = GetUserControl(m.DATACONTEXT);
                    }
                    //((BaseViewModel)((FrameworkElement)content).DataContext).MID = m.MID;
                    LayoutAnchorable la = new LayoutAnchorable();
                    la.Content = content;
                    la.IsActive = true;
                    la.Title = mi.Header.ToString();
                    LayDocPane.Children.Add(la);
                }
                else if (content.GetType().BaseType == typeof(Window))
                {
                    if (!string.IsNullOrEmpty(m.DATACONTEXT))
                    {
                        ((FrameworkElement)content).DataContext = GetUserControl(m.DATACONTEXT);
                    }
                    //((BaseViewModel)((FrameworkElement)content).DataContext).MID = m.MID;
                    ((Window)content).Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IMS - Parking Management Software", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PrintDaySummary()
        {
            DateTime TDATE;
            //if (GlobalClass.User.UserName.ToLower() == "admin")
            //    TDATE = new Forms.wDatePicker().GetDate();
            //else
                TDATE = DateTime.Today;
            string strPrint = string.Empty;
            int PrintLen = 40;
            string str =
@"SELECT VT.[Description] VehicleType, COUNT(*) [Count] FROM ParkingInDetails PID 
JOIN VehicleType VT ON PID.VehicleType = VT.VTypeID
{0}
GROUP BY [Description]";
            string strBase = string.Format(str,
@"LEFT JOIN ParkingOutDetails POD ON PID.PID = POD.PID
{0}");
            string strOpening = string.Format(strBase, "WHERE POD.PID IS NULL AND InDate < @TDATE");
            string strMissed = string.Format(strBase, "WHERE POD.PID IS NULL AND InDate = @TDATE");
            string strInCount = string.Format(str, "WHERE InDate = @TDATE");
            string strOutCount = string.Format(strBase, "WHERE OutDate = @TDATE");
            string strOutFromPrevDates = string.Format(strBase, "WHERE OutDate = @TDATE AND InDate < @TDATE");

            string strScanLog =
@"SELECT VT.[Description] VehicleType, COUNT(*) [Count], SUM(CL.ChargedAmount) Amount FROM ParkingInDetails PID 
JOIN VehicleType VT ON PID.VehicleType = VT.VTypeID
LEFT JOIN POUT_CLEARLOG CL ON PID.PID = CL.PID
LEFT JOIN ParkingOutDetails POD ON PID.PID = POD.PID
WHERE CL.PID IS NOT NULL AND CL.OutDate = @TDATE AND POD.PID IS NULL
GROUP BY [Description]";


            string strCollection =
@"SELECT ENTERED_BY, SUM(TAXABLE_AMOUNT + TAX_AMOUNT) [Collection] FROM VIEW_Annex7 WHERE BILL_DATE = @TDATE AND LEFT(BILL_NO,2) IN ('SI', 'TI')
GROUP BY ENTERED_BY";

            string strSalesReturn =
@"SELECT ENTERED_BY, SUM(TAXABLE_AMOUNT + TAX_AMOUNT) [Return] FROM VIEW_Annex7 WHERE BILL_DATE = @TDATE AND LEFT(BILL_NO,2) IN ('CN')
GROUP BY ENTERED_BY";

            try
            {
                strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, 45) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
                strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, 45) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
                strPrint += "DAY SUMMARY REPORT".PadLeft(30, ' ') + Environment.NewLine;

                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    //---------------OPENING VEHICLE COUNT------------
                    var OpeningList = conn.Query(strOpening, new { TDATE = TDATE });
                    if (OpeningList != null && OpeningList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Opening Vehilce Count" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in OpeningList)
                        {
                            strPrint += model.VehicleType.PadRight(PrintLen - 15, ' ') + ":" + model.Count.ToString().PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------IN VEHICLE COUNT --------------------
                    var EntryList = conn.Query(strInCount, new { TDATE = TDATE });
                    if (EntryList != null && EntryList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Vehilce Entry Count" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in EntryList)
                        {
                            strPrint += model.VehicleType.PadRight(PrintLen - 15, ' ') + ":" + model.Count.ToString().PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------OUT VEHICLE COUNT --------------------
                    var ExitList = conn.Query(strOutCount, new { TDATE = TDATE });
                    if (ExitList != null && ExitList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Vehilce Exit Count" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in ExitList)
                        {
                            strPrint += model.VehicleType.PadRight(PrintLen - 15, ' ') + ":" + model.Count.ToString().PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //-------------- VEHICLE EXIT COUNT THAT ENTERED PREVIOUS DATES--------------------
                    var ExitList1 = conn.Query(strOutFromPrevDates, new { TDATE = TDATE });
                    if (ExitList1 != null && ExitList1.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Vehilce Exit From Prev. Dates" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in ExitList1)
                        {
                            strPrint += model.VehicleType.PadRight(PrintLen - 15, ' ') + ":" + model.Count.ToString().PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------MISSED SLIPS-------------------- 
                    var MissedList = conn.Query(strMissed, new { TDATE = TDATE });
                    if (MissedList != null && MissedList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Missed Slip" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in MissedList)
                        {
                            strPrint += model.VehicleType.PadRight(PrintLen - 15, ' ') + ":" + model.Count.ToString().PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------SCAN LOG-------------------- 
                    var ScanList = conn.Query(strScanLog, new { TDATE = TDATE });
                    if (ScanList != null && ScanList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Scan Log" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in ScanList)
                        {
                            strPrint += model.VehicleType.PadRight(20, ' ') + ":" + model.Count.ToString().PadLeft(7, ' ') + ":" + model.Amount.ToString("#0.00").PadLeft(7, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------TOTAL COLLECTION-------------------- 
                    var CollectionList = conn.Query(strCollection, new { TDATE = TDATE });
                    if (CollectionList != null && CollectionList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Total Collection" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in CollectionList)
                        {
                            strPrint += model.ENTERED_BY.PadRight(PrintLen - 15, ' ') + ":" + model.Collection.ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }

                    //--------------TOTAL SALES RETURN--------------------
                    var ReturnList = conn.Query(strSalesReturn, new { TDATE = TDATE });
                    if (ReturnList != null && ReturnList.Count() > 0)
                    {
                        strPrint += Environment.NewLine + "Total Sales Return" + Environment.NewLine;
                        strPrint += "".PadRight(PrintLen - 4, '-') + Environment.NewLine;
                        foreach (dynamic model in ReturnList)
                        {
                            strPrint += model.ENTERED_BY.PadRight(PrintLen - 15, ' ') + ":" + model.Collection.ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                        }
                    }
                    RawPrintFunctions.RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Day Summary");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IMS - Parking Management Software", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LayDocPane_ChildrenCollectionChanged(object sender, EventArgs e)
        {
            if (((LayoutDocumentPane)sender).Children.Count > 0)
            {
                spIMS.Visibility = Visibility.Collapsed;
            }
            else
            {
                spIMS.Visibility = Visibility.Visible;
            }
        }

        private void Reports(string p)
        {
            if (p == "TRANSACTION ACTIVITY LOG")
            {
                new Forms.Reports.wTranLogReport().Show();
                return;
            }
            LayoutAnchorable la = new LayoutAnchorable();
            la.IsActive = true;
            switch (p)
            {
                case "SUMMARY":
                    la.Content = new Forms.Reports.RePrintLogReport(2);
                    la.Title = "ABBREVIATED SALES REGISTER REPORT - SUMMARY";

                    break;
                case "DETAILS":
                    la.Content = new Forms.Reports.RePrintLogReport(3);
                    la.Title = "ABBREVIATED SALES REGISTER REPORT - DETAILS";
                    break;
                case "VAT SALES REGISTER REPORT":
                    la.Content = new Forms.Reports.RePrintLogReport(4);
                    la.Title = "VAT SALES REGISTER REPORT";
                    break;
                case "CREDIT NOTE REGISTER REPORT":
                    la.Content = new Forms.Reports.RePrintLogReport(5);
                    la.Title = "CREDIT NOTE REGISTER REPORT";
                    break;
                case "PURCHASE REGISTER REPORT":
                    la.Content = new Forms.Reports.RePrintLogReport(6);
                    la.Title = "PURCHASE REGISTER REPORT";
                    break;
                case "SALES INVOICE REPRINT LOG":
                    la.Content = new Forms.Reports.RePrintLogReport(0);
                    la.Title = "SALES INVOICE REPRINT LOG";
                    break;
                case "ANNEX 7 REPORT":
                    la.Content = new Forms.Reports.RePrintLogReport(1);
                    la.Title = "ANNEX 7 REPORT";
                    break;
                case "VOUCHER DISCOUNT REPORTS - SUMMARY":
                    la.Content = new Forms.Reports.RePrintLogReport(8);
                    la.Title = p;
                    break;
                case "VOUCHER DISCOUNT REPORTS - DETAILS":
                    la.Content = new Forms.Reports.RePrintLogReport(7);
                    la.Title = p;
                    break;
                case "SETTLEMENT REPORT":
                    la.Content = new Forms.Reports.ucSettlementReport();
                    la.Title = p;
                    break;
                case "PARKING PASS REPORT - SUMMARY":
                    la.Content = new Forms.Reports.RePrintLogReport(10);
                    la.Title = p;
                    break;
                case "PARKING PASS REPORT - DETAILS":
                    la.Content = new Forms.Reports.RePrintLogReport(9);
                    la.Title = p;
                    break;
                default:
                    return;
            }

            LayDocPane.Children.Add(la);
        }

        private object GetUserControl(string FormPath)
        {
            Type FormType = Type.GetType(FormPath);
            if (FormType != null)
            {
                ConstructorInfo Cinfo = FormType.GetConstructor(Type.EmptyTypes);
                return Cinfo.Invoke(null);
                //if (FormType.BaseType == typeof(UserControl))
                //{

                //    UserControl MyControl = (UserControl)Cinfo.Invoke(null);
                //    MyControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                //    return MyControl;
                //}
                //else if (FormType.BaseType == typeof(Window))
                //{
                //    //ConstructorInfo Cinfo = FormType.GetConstructor(Type.EmptyTypes);
                //    Window MyControl = (Window)Cinfo.Invoke(null);
                //    MyControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                //    return MyControl;
                //}
                //else
                //{
                //    return null;
                //}
            }
            else
            {
                return null;
            }
        }
        private void LoadMenu(MenuItem mi, int parent)
        {
            foreach (TMenu child in _MenuList.Where(x => x.PARENT == parent))
            {
                MenuItem chMenu = new MenuItem();
                chMenu.Tag = child;
                chMenu.Header = child.MENUNAME;
                if (!string.IsNullOrEmpty(child.IMAGEPATH))
                {
                    Image icon = new Image();
                    icon.Source = Imaging.FileToImage(child.IMAGEPATH);
                    chMenu.Icon = icon;
                }
                chMenu.Click += mi_Click;
                LoadMenu(chMenu, child.MID);
                mi.Items.Add(chMenu);
            }
        }



        private void LogOut()
        {
            if (MessageBox.Show("Do you want to log out of System?", "Logging Out", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            this.Closing -= MainWindow_Closing;
            GlobalClass.EndSession();
            GlobalClass.User = null;
            this.Close();
            new Forms.File.Login().Show();
        }

        private void Exit()
        {
            if (MessageBox.Show("Do you want to close the application. Any unsaved activities would be discarded?", "Application Closing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.Closing -= MainWindow_Closing;
                GlobalClass.EndSession();
                Application.Current.Shutdown();
            }
        }


    }
}
