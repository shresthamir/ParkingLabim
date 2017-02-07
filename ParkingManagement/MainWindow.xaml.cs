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
                        case "DATA BACKUP":
                            if (IsAppRunningInServer)
                            {
                                wDataBackup w = new wDataBackup();
                                w.ShowDialog();
                                return;
                            }
                            //}
                            //ProductMasterServiceSoapClient Prod_Operations = new ProductMasterServiceSoapClient(new System.ServiceModel.BasicHttpBinding() { MaxReceivedMessageSize = int.MaxValue }, new System.ServiceModel.EndpointAddress("http://" + GlobalClass.ServerUrl + "/ProductMasterService.asmx"));
                            //ServiceResult sr = Prod_Operations.DataBackup();
                            //if (sr.Result)
                            //{
                            //    Prod_Operations.SetUserActivityLog(GlobalClass.CurUser.UserName, GlobalClass.CurUser.Password, GlobalClass.CurUser.UserSessId, "Database Backup", "Backup", string.Empty, string.Empty, string.Empty);
                            //}
                            //MessageBox.Show(sr.Message, "Data Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        case "DATA RECOVERY":
                            if (IsAppRunningInServer)
                            {
                                new wDataRestore().ShowDialog();
                            }
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
                MessageBox.Show(ex.Message, "IMS POS", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if(p=="TRANSACTION ACTIVITY LOG")
            {
                new Forms.Reports.wTranLogReport().Show();
                return;
            }
            LayoutAnchorable la = new LayoutAnchorable();
            la.IsActive = true;            
            switch(p)
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
