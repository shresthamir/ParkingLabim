using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
namespace ParkingManagement.Models
{
    public class Holiday : BaseModel
    {
        private string _Holiday_Name;
        private DateTime _Holiday_Date;
        private int _id;

        public DateTime HolidayDate { get { return _Holiday_Date; } set { _Holiday_Date = value; OnPropertyChanged("HolidayDate"); } }
        public string HolidayName { get { return _Holiday_Name; } set { _Holiday_Name = value; OnPropertyChanged("HolidayName"); } }
        public int HolidayId { get { return _id; } set { _id = value; OnPropertyChanged("HolidayId"); } }


        public override bool Save(SqlTransaction tran)
        {
            string strSql = "INSERT INTO Holiday (HolidayId, HolidayName, HolidayDate) VALUES (@HolidayId, @HolidayName, @HolidayDate)";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            string strSql = "UPDATE Holiday SET  HolidayName = @HolidayName, HolidayDate = @HolidayDate WHERE HolidayId = @HolidayId";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            string strSql = "DELETE FROM Holiday WHERE HolidayId = @HolidayId";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }
    }
}
