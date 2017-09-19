using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
namespace ParkingManagement.Models
{
    public class Shift:BaseModel
    {
        #region Members

        private short _SHIFT_ID;
        private string _SHIFT_NAME;
        private DateTime _SHIFT_START;
        private DateTime _SHIFT_END;
        private byte _SHIFT_STATUS;
        private int _UID;
        #endregion

        #region Properties

        public short SHIFT_ID { get { return _SHIFT_ID; } set { _SHIFT_ID = value; OnPropertyChanged("SHIFT_ID"); } }

        public string SHIFT_NAME { get { return _SHIFT_NAME; } set { _SHIFT_NAME = value; OnPropertyChanged("SHIFT_NAME"); } }

        public DateTime SHIFT_START { get { return _SHIFT_START; } set { _SHIFT_START = value; OnPropertyChanged("SHIFT_START"); } }

        public DateTime SHIFT_END { get { return _SHIFT_END; } set { _SHIFT_END = value; OnPropertyChanged("SHIFT_END"); } }

        public byte SHIFT_STATUS { get { return _SHIFT_STATUS; } set { _SHIFT_STATUS = value; OnPropertyChanged("SHIFT_STATUS"); } }        
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }

        #endregion

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            SHIFT_START = DateTime.Today.Date.Add(SHIFT_START.TimeOfDay);
            SHIFT_END = DateTime.Today.Date.Add(SHIFT_END.TimeOfDay);
            UID = Library.GlobalClass.User.UID;
            string strSave = @"INSERT INTO tblShift(SHIFT_ID, SHIFT_NAME, SHIFT_START, SHIFT_END, SHIFT_STATUS, UID) 
			                        VALUES (@SHIFT_ID, @SHIFT_NAME, @SHIFT_START, @SHIFT_END, @SHIFT_STATUS, @UID)";
            return tran.Connection.Execute(strSave, this, tran) == 1;
        }

        public override bool Update(System.Data.SqlClient.SqlTransaction tran)
        {
            SHIFT_START = DateTime.Today.Date.Add(SHIFT_START.TimeOfDay);
            SHIFT_END = DateTime.Today.Date.Add(SHIFT_END.TimeOfDay);
            UID = Library.GlobalClass.User.UID;
            string strUpdate = @"UPDATE tblShift SET SHIFT_NAME = @SHIFT_NAME, SHIFT_START = @SHIFT_START, SHIFT_END = @SHIFT_END, SHIFT_STATUS = @SHIFT_STATUS, UID = @UID WHERE SHIFT_ID = @SHIFT_ID";
            return tran.Connection.Execute(strUpdate, this, tran) == 1;
        }

        public override bool Delete(System.Data.SqlClient.SqlTransaction tran)
        {
            string strDelete = "DELETE FROM tblShift WHERE SHIFT_ID = @SHIFT_ID";
            return tran.Connection.Execute(strDelete, this, tran) == 1;
        }
    }

}
