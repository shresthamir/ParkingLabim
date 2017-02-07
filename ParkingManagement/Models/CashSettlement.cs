using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
namespace ParkingManagement.Models
{

    class TCashSettlement : BaseModel
    {
        #region Members
        private int _ID;
        public DateTime _TRNDATE;
        private decimal _AMOUNT;
        private string _TERMINAL;
        private int _SETTLED_UID;
        private string _TRNTIME;
        private int _SETTLER_UID;
        private byte _POST;
        private decimal _CollectionAmount;
        private byte _FYID;
        #endregion

        #region Properties

        public int SETTLEMENT_ID { get { return _ID; } set { _ID = value; OnPropertyChanged("SETTLEMENT_ID"); } }
        public DateTime TRNDATE { get { return _TRNDATE; } set { _TRNDATE = value; OnPropertyChanged("TRNDATE"); } }
        public decimal AMOUNT { get { return _AMOUNT; } set { _AMOUNT = value; OnPropertyChanged("AMOUNT"); } }
        public string TERMINAL_CODE { get { return _TERMINAL; } set { _TERMINAL = value; OnPropertyChanged("TERMINAL_CODE"); } }
        public int SETTLED_UID { get { return _SETTLED_UID; } set { _SETTLED_UID = value; OnPropertyChanged("SETTLED_UID"); } }
        public string TRNTIME { get { return _TRNTIME; } set { _TRNTIME = value; OnPropertyChanged("TRNTIME"); } }
        public int SETTLER_UID { get { return _SETTLER_UID; } set { _SETTLER_UID = value; OnPropertyChanged("SETTLER_UID"); } }
        public byte POST { get { return _POST; } set { _POST = value; OnPropertyChanged("POST"); } }
        public byte FYID { get { return _FYID; } set { _FYID = value; OnPropertyChanged("FYID"); } }
        public decimal CollectionAmount { get { return _CollectionAmount; } set { _CollectionAmount = value; OnPropertyChanged("CollectionAmount"); } }
        #endregion


        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSave = "INSERT INTO CashSettlement(SETTLEMENT_ID, TRNDATE, AMOUNT, TERMINAL_CODE, SETTLED_UID, TRNTIME, SETTLER_UID, POST, CollectionAmount, FYID) VALUES(@SETTLEMENT_ID, @TRNDATE, @AMOUNT, @TERMINAL_CODE, @SETTLED_UID, @TRNTIME, @SETTLER_UID, @POST, @CollectionAmount, @FYID)";
            return tran.Connection.Execute(strSave, this, tran) == 1;
        }
    }

    class CashSettlement : TCashSettlement
    {
        private User _Settled_User;
        private User _Settler_User;
        private Terminal _Terminal;
        public Terminal Terminal { get { return _Terminal; } set { _Terminal = value; OnPropertyChanged("Terminal"); } }
        public User Settler_User { get { return _Settler_User; } set { _Settler_User = value; OnPropertyChanged("Settler_User"); } }
        public User Settled_User { get { return _Settled_User; } set { _Settled_User = value; OnPropertyChanged("Settled_User"); } }
    }

    class Denomination : BaseModel
    {

        #region Members
        private decimal _R1000;
        private decimal _R500;
        private decimal _R250;
        private decimal _R100;
        private decimal _R50;
        private decimal _R25;
        private decimal _R20;
        private decimal _RTOTAL;
        private decimal _RIC;
        private decimal _R05;
        private decimal _R1;
        private decimal _R2;
        private decimal _R5;
        private decimal _R10;
        private int _ID;
        private byte _FYID;
        #endregion
        #region Property
        public int SETTLEMENT_ID { get { return _ID; } set { _ID = value; OnPropertyChanged("SETTLEMENT_ID"); } }
        public byte FYID { get { return _FYID; } set { _FYID = value; OnPropertyChanged("FYID"); } }
        public decimal R1000 { get { return _R1000; } set { _R1000 = value; OnPropertyChanged("R1000"); } }
        public decimal R500 { get { return _R500; } set { _R500 = value; OnPropertyChanged("R500"); } }
        public decimal R250 { get { return _R250; } set { _R250 = value; OnPropertyChanged("R250"); } }
        public decimal R100 { get { return _R100; } set { _R100 = value; OnPropertyChanged("R100"); } }
        public decimal R50 { get { return _R50; } set { _R50 = value; OnPropertyChanged("R50"); } }
        public decimal R25 { get { return _R25; } set { _R25 = value; OnPropertyChanged("R25"); } }
        public decimal R20 { get { return _R20; } set { _R20 = value; OnPropertyChanged("R20"); } }
        public decimal R10 { get { return _R10; } set { _R10 = value; OnPropertyChanged("R10"); } }
        public decimal R5 { get { return _R5; } set { _R5 = value; OnPropertyChanged("R5"); } }
        public decimal R2 { get { return _R2; } set { _R2 = value; OnPropertyChanged("R2"); } }
        public decimal R1 { get { return _R1; } set { _R1 = value; OnPropertyChanged("R1"); } }
        public decimal R05 { get { return _R05; } set { _R05 = value; OnPropertyChanged("R05"); } }
        public decimal RIC { get { return _RIC; } set { _RIC = value; OnPropertyChanged("RIC"); } }
        public decimal RTOTAL { get { return _RTOTAL; } set { _RTOTAL = value; OnPropertyChanged("RTOTAL"); } }
        #endregion

        public Denomination()
        {
            this.PropertyChanged += Denomination_PropertyChanged;
        }

        void Denomination_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "RTOTAL" && e.PropertyName != "USERID" && e.PropertyName != "TRNDATE" && e.PropertyName != "COUNTER")
                RTOTAL = RIC * (decimal)1.6 + R1000 * 1000 + R500 * 500 + R250 * 250 + R100 * 100 + R50 * 50 + R25 * 25 + R20 * 20 + R10 * 10 + R5 * 5 + R2 * 2 + R1 + R05 * (decimal)0.5;
        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSave = "INSERT INTO Denomination(SETTLEMENT_ID, R1000, R500, R250, R100, R50, R25, R20, R10, R5, R2, R1, R05, RIC, RTOTAL, FYID) VALUES(@SETTLEMENT_ID, @R1000, @R500, @R250, @R100, @R50, @R25, @R20, @R10, @R5, @R2, @R1, @R05, @RIC, @RTOTAL, @FYID)";
            return tran.Connection.Execute(strSave, this, tran) == 1;
        }
    }
}
