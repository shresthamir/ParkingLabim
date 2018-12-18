using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ParkingManagement.Library;
using System.Windows;
using Microsoft.Win32;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.OleDb;
using ParkingManagement.Models;
using ParkingManagement.Library.Helpers;
using System.ComponentModel;

namespace ParkingManagement.ViewModel
{
    class CustomerViewModel : Library.Helpers.BaseViewModel
    {

        //private int _CustomerId;
        private Customer _Customer;
        private ObservableCollection<Customer> _CustomerList;
        private Customer _SelectedCustomer;
       

        //public int CustomerId { get { return _CustomerId; } set { _CustomerId = value; OnPropertyChanged("CustomerId"); } }
        public Customer Customer { get { return _Customer; } set { _Customer = value; OnPropertyChanged("Customer"); } }
        public Customer SelectedCustomer { get { return _SelectedCustomer; } set { _SelectedCustomer = value; OnPropertyChanged("SelectedCustomer"); } }
        public ObservableCollection<Customer> CustomerList { get { return _CustomerList; } set { _CustomerList = value; OnPropertyChanged("CustomerList"); } }
       


    public CustomerViewModel()
    {
        try
        {
            NewCommand = new RelayCommand(NewMethod);
            SaveCommand = new RelayCommand(SaveMethod);
            EditCommand = new RelayCommand(EditMethod);
            UndoCommand = new RelayCommand(UndoMethod);
            DeleteCommand = new RelayCommand(DeleteMethod);
            LoadData = new RelayCommand(LoadMethod);
            MessageBoxCaption = "Customer Registration";

            UndoMethod(null);
           
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
        

    protected void DeleteMethod(object obj)
    {
        try
        {
            if (MessageBox.Show("You are going to Delete this Customer.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    Customer.Delete(tran);
                    tran.Commit();
                }
            }
            CustomerList.Remove(CustomerList.First(x => x.CustomerId == Customer.CustomerId));
            MessageBox.Show("Customer Successfully Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
            UndoMethod(null);
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void EditMethod(object obj)
    {
        SetAction(ButtonAction.Edit);
    }

    protected void LoadMethod(object obj)
    {
        try
        {
            if (SelectedCustomer != null)
            {
                Customer = new Customer()
                {
                    CustomerId = SelectedCustomer.CustomerId,
                    CustomerName = SelectedCustomer.CustomerName,
                    Address = SelectedCustomer.Address,
                    Mobile = SelectedCustomer.Mobile,
                   Code=SelectedCustomer.Code,
                   Pan=SelectedCustomer.Pan,
                   ContactName=SelectedCustomer.ContactName,
                   Email=SelectedCustomer.Email,
                };
                SetAction(ButtonAction.Selected);
            }
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected void NewMethod(object obj)
    {
        try
        {
            UndoMethod(null);
              
    
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    //Customer.CustomerList = new ObservableCollection<Customer>(conn.Query<Customer>("select CustomerId,CustomerName,ContactName,Code,Mobile from customer"));
        
                    Customer.CustomerId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(CustomerId), 0) + 1 FROM Customer");
                }
                SetAction(ButtonAction.New);
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

        private void Customer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void SaveMethod(object obj)
    {
        if (!string.IsNullOrEmpty(Customer.Error))
        {
            MessageBox.Show(Customer.Error, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (_action == ButtonAction.New)
            Save(Customer);
        else if (_action == ButtonAction.Edit)
            UpdateCustomer(Customer);
    }

    private void Save(object obj)
    {
        try
        {
            if (MessageBox.Show("You are going to Save this Customer.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                        Customer.CustomerId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(CustomerId), 0) + 1 FROM Customer", transaction: tran);
                        Customer.Save(tran);
                    tran.Commit();
                }
            }
            CustomerList.Add(new Customer()
            {
                CustomerId = Customer.CustomerId,
                Code = Customer.Code,
                CustomerName = Customer.CustomerName,
                Address = Customer.Address,
                Mobile = Customer.Mobile, 
               Pan=Customer.Pan,
               ContactName=Customer.ContactName,
               Email=Customer.Email,
            });
            MessageBox.Show("Customer successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
            UndoMethod(null);

        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void UpdateCustomer(object obj)
    {
        try
        {
            if (MessageBox.Show("You are going to Update this Customer.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    Customer.Update(tran);
                    tran.Commit();
                }
            }
            SelectedCustomer.CustomerName = Customer.CustomerName;
            SelectedCustomer.Address = Customer.Address;
            SelectedCustomer.Mobile = Customer.Mobile;
            SelectedCustomer.Pan = Customer.Pan;
            SelectedCustomer.ContactName = Customer.ContactName;
            SelectedCustomer.Email = Customer.Email;
            SelectedCustomer.Code = Customer.Code;
            MessageBox.Show("Customer Successfully Updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
            UndoMethod(null);
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected void UndoMethod(object obj)
    {
        try
        {
            SelectedCustomer = null;
            Customer = new Customer();
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    CustomerList = new ObservableCollection<Customer>(conn.Query<Customer>("SELECT * FROM Customer"));
                    //CustomerList = new ObservableCollection<Customer>(conn.Query<Customer>("SELECT * FROM Customer"));
                }
                SetAction(ButtonAction.Init);
        }
        catch (Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
   
}
