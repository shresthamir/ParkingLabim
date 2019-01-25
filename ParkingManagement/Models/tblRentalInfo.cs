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
    class tblRentalInfo : BaseModel, IDataErrorInfo
    {
   
        private String _CustomerId;
        private int _BillMonth;
        private int _BillYear;
        private decimal _BillAmount;
        
      
        public String CustomerId { get { return _CustomerId; } set { _CustomerId = value; OnPropertyChanged("CustomerId"); } }
        public int BillMonth { get { return _BillMonth; } set { _BillMonth = value; OnPropertyChanged("BillMonth"); } }
        public int BillYear { get { return _BillYear; } set { _BillYear = value; OnPropertyChanged("BillYear"); } }
        public decimal BillAmount { get { return _BillAmount; } set { _BillAmount = value; OnPropertyChanged("BillAmount"); } }

        string IDataErrorInfo.Error => throw new NotImplementedException();

        string IDataErrorInfo.this[string columnName] => throw new NotImplementedException();

        public override bool Save(SqlTransaction tran)
        {
            return tran.Connection.Execute("INSERT INTO tblRentalInfo(CustomerId, BillMonth, BillYear, BillAmount) VALUES (@CustomerId,@BillMonth, @BillYear, @BillAmount)", this, tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            return tran.Connection.Execute("Update tblRentalInfo SET CustomerId = @CustomerId, BillMonth=@BillMonth, BillYear=@BillYear, BillAmount=@BillAmount WHERE CustomerId = @CustomerId", this, tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            return tran.Connection.Execute("DELETE FROM tblRentalInfo WHERE CustomerId = @CustomerId", this, tran) == 1;
        }
    }

}
