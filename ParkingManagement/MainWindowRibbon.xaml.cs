using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
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

namespace ParkingManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();
            RVehicleType.Visibility = (GlobalClass.User.VehicleType) ? Visibility.Visible : Visibility.Collapsed;
            RRate.Visibility = (GlobalClass.User.RateMaster) ? Visibility.Visible : Visibility.Collapsed;
            RParty.Visibility = (GlobalClass.User.PartySetting) ? Visibility.Visible : Visibility.Collapsed;
            RUserSetting.Visibility = (GlobalClass.User.UserSetting) ? Visibility.Visible : Visibility.Collapsed;
            RParkingIn.Visibility = (GlobalClass.User.ParkingIn) ? Visibility.Visible : Visibility.Collapsed;
            RParkingOut.Visibility = (GlobalClass.User.ParkingOut) ? Visibility.Visible : Visibility.Collapsed;
            RCashReceipt.Visibility = (GlobalClass.User.CashReceipt) ? Visibility.Visible : Visibility.Collapsed;
            Reports.Visibility = (GlobalClass.User.Reports) ? Visibility.Visible : Visibility.Collapsed;
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            object[] customAttributes = entryAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            //MessageBox.Show(((AssemblyConfigurationAttribute)customAttributes[0]).Configuration);
         // VehicleType.Visibility = (GlobalClass.User.VehicleType) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Window_Docking_Billing_ActiveContentChanged(object sender, EventArgs e)
        {

        }

        private void LayDocPane_ChildrenCollectionChanged(object sender, EventArgs e)
        {

        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var activeChild= LayDocPane.Children.FirstOrDefault(x => x.IsSelected);
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
            else
            {
                LayoutAnchorable la = new LayoutAnchorable();
                switch(e.Key)
                {
                    case Key.F1:
                        if (GlobalClass.User.VehicleType)
                        {
                            la.Content = new Forms.Master.Vehicle_Type.ucVehicleType();
                            la.Title = "Vehicle Type";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F2:
                        if (GlobalClass.User.RateMaster)
                        {
                            //la.Content = new Forms.Reports.Cash_Collection_Reports.CCVehicleWise();
                            la.Content = new Forms.Master.Rate.ucRate();
                            la.Title = "Rate Setting";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F3:
                        if (GlobalClass.User.PartySetting)
                        {
                            la.Content = new Forms.Master.Party_Setting.ucParty();
                            la.Title = "Party Setting";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F4:
                        if (GlobalClass.User.UserSetting)
                        {
                            la.Content = new Forms.Master.User_Setting.UserSetting();
                            la.Title = "User Setting";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F5:
                        if (GlobalClass.User.ParkingIn)
                        {
                            la.Content = new Forms.Transaction.Parking_In.ucParkingIn();
                            la.Title = "Parking In";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F6:
                        if (GlobalClass.User.ParkingOut)
                        {
                            la.Content = new Forms.Transaction.Parking_Out.ucParkingOut();
                            la.Title = "Parking Out";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F7: if (GlobalClass.User.CashReceipt)
                        {
                            la.Content = new Forms.Transaction.Cash_Receipt.CashReceipt();
                            la.Title = "Cash Receipt";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                    case Key.F8: if (GlobalClass.User.Reports)
                        {
                            la.Content = new Forms.Reports.ucDailySalesReport();
                            la.Title = "Report";
                            la.IsActive = true;
                            LayDocPane.Children.Add(la);
                        }
                        break;
                }
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            new Forms.File.ChangePassword().ShowDialog();
        }

        private void PrinterSetting_Click(object sender, RoutedEventArgs e)
        {
            new Forms.File.PrinterSetting().ShowDialog();
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            GlobalClass.User = null;
            this.Close();
            new Forms.File.Login().Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void VehicleType_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Master.Vehicle_Type.ucVehicleType();
            la.Title = "Vehicle Type";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void Rate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Master.Rate.ucRate();
            la.Title = "Rate Setting";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void Party_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Master.Party_Setting.ucParty();
            la.Title = "Party Setting";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void UserSetting_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Master.User_Setting.UserSetting();
            la.Title = "User Setting";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void ParkingIn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Transaction.Parking_In.ucParkingIn();
            la.Title = "Parking In";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void ParkingOut_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Transaction.Parking_Out.ucParkingOut();
            la.Title = "Parking Out";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void CashReceipt_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Transaction.Cash_Receipt.CashReceipt();
            la.Title = "Cash Receipt";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void SalesReports_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Reports.ucDailySalesReport();
            la.Title = "Cash Receipt";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void PartyLedger_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Reports.ucPartyLedger();
            la.Title = "Party Ledger";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }

        private void ParkingReport_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutAnchorable la = new LayoutAnchorable();
            la.Content = new Forms.Reports.ucParkingReport();
            la.Title = "Parking Report";
            la.IsActive = true;
            LayDocPane.Children.Add(la);
        }
    }
}
