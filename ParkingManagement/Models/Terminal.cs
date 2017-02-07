using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ParkingManagement.Library;

namespace ParkingManagement.Models
{
    public class Terminal : BaseModel, IDataErrorInfo
    {
        private int _UID;
        private byte _STATUS;
        private string _TERMINAL_NAME;
        private string _TERMINAL_CODE;
        //   private short _TERMINAL_ID;
        //  public short TERMINAL_ID { get { return _TERMINAL_ID; } set { _TERMINAL_ID = value; OnPropertyChanged("TERMINAL_ID"); } }
        public string TERMINAL_CODE { get { return _TERMINAL_CODE; } set { _TERMINAL_CODE = value; OnPropertyChanged("TERMINAL_CODE"); } }
        public string TERMINAL_NAME { get { return _TERMINAL_NAME; } set { _TERMINAL_NAME = value; OnPropertyChanged("TERMINAL_NAME"); } }
        public byte STATUS { get { return _STATUS; } set { _STATUS = value; OnPropertyChanged("STATUS"); } }
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }

        public Terminal()
        {
            UID = GlobalClass.User.UID;
        }
        public string Error
        {
            get
            {
                if (string.IsNullOrEmpty(TERMINAL_CODE))
                    return "Terminal Code cannot be empty!";
                else if (TERMINAL_CODE.Length != 3)
                    return "Terminal Code must be 3 characters long!";
                if (string.IsNullOrEmpty(TERMINAL_NAME))
                    return "Terminal Name cannot be empty!";
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
                    case "TERMINAL_CODE":
                        if (string.IsNullOrEmpty(TERMINAL_CODE))
                            Result = "Terminal Code cannot be empty!";
                        else if (TERMINAL_CODE.Length != 3)
                            Result = "Terminal Code must be 3 characters long!";
                        break;
                    case "TERMINAL_NAME":
                        if (string.IsNullOrEmpty(TERMINAL_NAME))
                            Result = "Terminal Name cannot be empty!";
                        break;
                }
                return Result;
            }
        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSave = "INSERT INTO TERMINALS (TERMINAL_CODE, TERMINAL_NAME, [STATUS], [UID]) VALUES (@TERMINAL_CODE, @TERMINAL_NAME, @STATUS, @UID)";
            return tran.Connection.Execute(strSave, this, tran) == 1;
        }

        public override bool Update(System.Data.SqlClient.SqlTransaction tran)
        {
            string strUpdate = "UPDATE TERMINALS SET TERMINAL_NAME = @TERMINAL_NAME, [STATUS] = @STATUS, [UID] = @UID WHERE TERMINAL_CODE = @TERMINAL_CODE";
            return tran.Connection.Execute(strUpdate, this, tran) == 1;
        }

        public override bool Delete(System.Data.SqlClient.SqlTransaction tran)
        {
            string strDelete = "DELETE FROM TERMINALS WHERE TERMINAL_CODE = @TERMINAL_CODE";
            return tran.Connection.Execute(strDelete, this, tran) == 1;
        }
    }
}
