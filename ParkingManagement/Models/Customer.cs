using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;

namespace ParkingManagement.Models
{
    class Customer : BaseModel, IDataErrorInfo
    {
        private int _CustomerId;
        private string _Code;
        private string _CustomerName;
        private string _Address = string.Empty;
        private string _Mobile;
        private string _Pan;
        private string _ContactName;
        private string _Email;
       // private ObservableCollection<CustomerList> _CustomerList;

        public int CustomerId { get { return _CustomerId; } set { _CustomerId = value; OnPropertyChanged("CustomerId"); } }
        public string Code { get { return _Code; } set { _Code = value; OnPropertyChanged("Code"); } }
        public string CustomerName { get { return _CustomerName; } set { _CustomerName = value; OnPropertyChanged("CustomerName"); } }
        public string Address { get { return _Address; } set { _Address = value; OnPropertyChanged("Address"); } }
        public string Mobile { get { return _Mobile; } set { _Mobile = value; OnPropertyChanged("Mobile"); } }
        public string Pan { get { return _Pan; } set { _Pan = value; OnPropertyChanged("Pan"); } }
        public string ContactName { get { return _ContactName; } set { _ContactName = value; OnPropertyChanged("ContactName"); } }
        public string Email { get { return _Email; } set { _Email = value; OnPropertyChanged("Email"); } }
        //public ObservableCollection<CustomerList> CustomerList { get { return _CustomerList; } set { _CustomerList = value; OnPropertyChanged("CustomerList"); } }



        public override bool Save(SqlTransaction tran)
        {
            return tran.Connection.Execute("INSERT INTO Customer(CustomerId, Code, CustomerName, Address, Mobile, Pan, ContactName, Email) VALUES (@CustomerId,@Code, @CustomerName, @Address, @Mobile, @Pan, @ContactName, @Email)", this, tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            return tran.Connection.Execute("Update Customer SET CustomerName = @CustomerName, Address = @Address, Mobile = @Mobile, Pan = @Pan, ContactName=@ContactName, Code = @Code WHERE CustomerId = @CustomerId", this, tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            return tran.Connection.Execute("DELETE FROM Customer WHERE CustomerId = @CustomerId", this, tran) == 1;
        }

        public string Error
        {
            get
            {
                
                 if (string.IsNullOrEmpty(Code))
                    return "Code cannot be empty";
                else if (string.IsNullOrEmpty(CustomerName))
                    return "Customer Name cannot be empty";
                else if (string.IsNullOrEmpty(Mobile))
                   return "Mobile No cannot be empty";
                else if (string.IsNullOrEmpty(ContactName))
                    return "Contact Person cannot be empty";
                
                return ""; 
            }

        }


        public string this[string columnName]
        {
            get
            {
                string Result = string.Empty;
                switch (columnName)
                {
                    case "CustomerName":
                        if (string.IsNullOrEmpty(CustomerName))
                            Result = "CustomerName cannot be empty";
                        break;
                   
                    case "Code":
                        if (string.IsNullOrEmpty(Code))
                            Result = "Code cannot be empty";
                        //else if (!Barcode.StartsWith("@"))
                           // Result = "Barcode must start with '@' character";
                        break;
                    case "Mobile":
                        if (string.IsNullOrEmpty(Mobile))
                            Result = "Mobile No cannot be empty";
                        break;
                  
                    case "Pan":
                        if (string.IsNullOrEmpty(Pan))
                            Result = "Pan cannot be empty.";
                        break;

                    case "ContactName":
                        if (string.IsNullOrEmpty(ContactName))
                            Result = "Contact Name cannot be empty";
                        break;

                    
                }
                return Result;
            }
        }

    }

    class Rent
    {
        public string Particulars { get; set; }
        public decimal Rate { get; set; }        
    }





}

