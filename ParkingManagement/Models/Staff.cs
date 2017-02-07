using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.ComponentModel;
namespace ParkingManagement.Models
{
    class Staff : BaseModel, IDataErrorInfo
    {
        private string _BARCODE;
        private string _FULLNAME;
        private string _ADDRESS;
        private string _DESIGNATION;
        private string _REMARKS;
        private byte _STATUS;

        public string BARCODE { get { return _BARCODE; } set { _BARCODE = value; OnPropertyChanged("BARCODE"); } }
        public string FULLNAME { get { return _FULLNAME; } set { _FULLNAME = value; OnPropertyChanged("FULLNAME"); } }
        public string ADDRESS { get { return _ADDRESS; } set { _ADDRESS = value; OnPropertyChanged("ADDRESS"); } }
        public string DESIGNATION { get { return _DESIGNATION; } set { _DESIGNATION = value; OnPropertyChanged("DESIGNATION"); } }
        public string REMARKS { get { return _REMARKS; } set { _REMARKS = value; OnPropertyChanged("REMARKS"); } }
        public byte STATUS { get { return _STATUS; } set { _STATUS = value; OnPropertyChanged("STATUS"); } }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            return tran.Connection.Execute("INSERT INTO tblStaff (BARCODE, FULLNAME, ADDRESS, DESIGNATION, REMARKS, STATUS) VALUES (@BARCODE, @FULLNAME, @ADDRESS, @DESIGNATION, @REMARKS, @STATUS)", this, tran) == 1;
        }

        public override bool Update(System.Data.SqlClient.SqlTransaction tran)
        {
            return tran.Connection.Execute("UPDATE tblStaff SET FULLNAME = @FULLNAME, ADDRESS = @ADDRESS, DESIGNATION = @DESIGNATION, REMARKS = @REMARKS, STATUS = @STATUS WHERE BARCODE = @BARCODE", this, tran) == 1;
        }
        public override bool Delete(System.Data.SqlClient.SqlTransaction tran)
        {
            return tran.Connection.Execute("DELETE FROM tblStaff WHERE BARCODE = @BARCODE", this, tran) == 1;
        }


        public string Error
        {
            get
            {
                if (string.IsNullOrEmpty(BARCODE))
                    return "Barcode cannot be empty";
                else if (string.IsNullOrEmpty(FULLNAME))
                    return "Staff Name cannot be empty";

                return string.Empty;
            }

        }

        public string this[string columnName]
        {
            get
            {
                string Result = string.Empty;
                switch (columnName)
                {
                    case "FULLNAME":
                        if (string.IsNullOrEmpty(FULLNAME))
                            Result = "Staff Name cannot be empty";
                        break;
                    case "BARCODE":
                        if (string.IsNullOrEmpty(BARCODE))
                            Result = "Barcode cannot be empty";
                        break;
                }
                return Result;
            }
        }
    }
}
