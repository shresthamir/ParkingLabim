using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using Dapper;
namespace ParkingManagement.Models
{

    public class TParkingIn : BaseModel
    {
        int _PID;
        byte _VehicleTypeID;
        DateTime _InDate;
        string _InMiti;
        string _Intime;
        string _PlateNo;
        string _Barcode;
        int _UID;
        private int _SESSION_ID;
        private byte _FYID;

        public int PID { get { return _PID; } set { _PID = value; OnPropertyChanged("PID"); } }
        public byte FYID { get { return _FYID; } set { _FYID = value; OnPropertyChanged("FYID"); } }
        public byte VehicleType { get { return _VehicleTypeID; } set { _VehicleTypeID = value; OnPropertyChanged("VehicleType"); } }
        public DateTime InDate { get { return _InDate; } set { _InDate = value; OnPropertyChanged("InDate"); } }
        public string InMiti { get { return _InMiti; } set { _InMiti = value; OnPropertyChanged("InMiti"); } }
        public string InTime { get { return _Intime; } set { _Intime = value; OnPropertyChanged("InTime"); } }
        public string PlateNo { get { return _PlateNo; } set { _PlateNo = value; OnPropertyChanged("PlateNo"); } }
        public string Barcode { get { return _Barcode; } set { _Barcode = value; OnPropertyChanged("Barcode"); } }
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }
        public int SESSION_ID { get { return _SESSION_ID; } set { _SESSION_ID = value; OnPropertyChanged("SESSION_ID"); } }


        public TParkingIn()
        {
            UID = GlobalClass.User.UID;
            SESSION_ID = GlobalClass.Session;
            FYID = GlobalClass.FYID;
        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSQL = @"INSERT INTO ParkingInDetails(PID, FYID, VehicleType, InDate, InTime, PlateNo, Barcode, [UID], InMiti, SESSION_ID)
                          Values(@PID, @FYID, @VehicleType,@InDate,@InTime,@PlateNo,@Barcode,@UID,@InMiti, @SESSION_ID)";
            return tran.Connection.Execute(strSQL, this, tran) >0;
        }
    }


    public class TParkingOut : BaseModel
    {
        int _PID;
        DateTime _OutDate;
        string _OutTime;
        string _OutMiti;
        string _Interval;
        int _Rate_ID;
        int _Party_ID;
        decimal _ChargedAmount;
        decimal _CashAmount;
        decimal _PartyAmount;
        string _Remarks;
        int _UID;
        private decimal _ChargedHours;
        private int _SESSION_ID;
        private byte _FYID;
        private string _STAFF_BARCODE;
        private string _BILLTO;
        private string _BILLTOADD;
        private string _BILLTOPAN;

        public int PID { get { return _PID; } set { _PID = value; OnPropertyChanged("PID"); } }
        public byte FYID { get { return _FYID; } set { _FYID = value; OnPropertyChanged("FYID"); } }
        public DateTime OutDate { get { return _OutDate; } set { _OutDate = value; OnPropertyChanged("OutDate"); } }
        public string OutMiti { get { return _OutMiti; } set { _OutMiti = value; OnPropertyChanged("OutMiti"); } }
        public string OutTime { get { return _OutTime; } set { _OutTime = value; OnPropertyChanged("OutTime"); } }
        public string Interval { get { return _Interval; } set { _Interval = value; OnPropertyChanged("Interval"); } }
        public int Rate_ID { get { return _Rate_ID; } set { _Rate_ID = value; OnPropertyChanged("Rate_ID"); } }
        public int Party_ID { get { return _Party_ID; } set { _Party_ID = value; OnPropertyChanged("Party_ID"); } }
        public decimal ChargedAmount { get { return _ChargedAmount; } set { _ChargedAmount = value; OnPropertyChanged("ChargedAmount"); } }
        public decimal CashAmount { get { return _CashAmount; } set { _CashAmount = value; OnPropertyChanged("CashAmount"); } }
        public decimal RoyaltyAmount { get { return _PartyAmount; } set { _PartyAmount = value; OnPropertyChanged("RoyaltyAmount"); } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; OnPropertyChanged("Remarks"); } }
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }
        public decimal ChargedHours { get { return _ChargedHours; } set { _ChargedHours = value; OnPropertyChanged("ChargedHours"); } }
        public int SESSION_ID { get { return _SESSION_ID; } set { _SESSION_ID = value; OnPropertyChanged("SESSION_ID"); } }        
        public string STAFF_BARCODE { get { return _STAFF_BARCODE; } set { _STAFF_BARCODE = value; OnPropertyChanged("STAFF_BARCODE"); } }
        public string BILLTO { get { return _BILLTO; } set { _BILLTO = value; OnPropertyChanged("BILLTO"); } }
        public string BILLTOADD { get { return _BILLTOADD; } set { _BILLTOADD = value; OnPropertyChanged("BILLTOADD"); } }
        public string BILLTOPAN { get { return _BILLTOPAN; } set { _BILLTOPAN = value; OnPropertyChanged("BILLTOPAN"); } }
        public TParkingOut()
        {
            UID = GlobalClass.User.UID;
            SESSION_ID = GlobalClass.Session;
            FYID = GlobalClass.FYID;
        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSQL = @"INSERT INTO ParkingOutDetails( PID, FYID, OutDate, OutMiti, OutTime, Interval, Rate_ID, ChargedAmount, CashAmount, RoyaltyAmount, Remarks, UID, ChargedHours, SESSION_ID, STAFF_BARCODE, BILLTO, BILLTOADD, BILLTOPAN) 
                                                                                VALUES(@PID, @FYID, @OutDate, @OutMiti, @OutTime, @Interval, @Rate_ID, @ChargedAmount, @CashAmount, @RoyaltyAmount, @Remarks, @UID, @ChargedHours, @SESSION_ID, @STAFF_BARCODE, @BILLTO, @BILLTOADD, @BILLTOPAN)";
            return tran.Connection.Execute(strSQL, this, transaction: tran) == 1;
        }

        public bool SaveLog(System.Data.SqlClient.SqlConnection conn)
        {
            string strSQL = @"INSERT INTO POUT_ClearLog( PID, FYID, OutDate, OutMiti, OutTime, Interval, Rate_ID, ChargedAmount, CashAmount, RoyaltyAmount, Remarks, UID, ChargedHours, SESSION_ID) 
                                                                                VALUES(@PID, @FYID, @OutDate, @OutMiti, @OutTime, @Interval, @Rate_ID, @ChargedAmount, @CashAmount, @RoyaltyAmount, @Remarks, @UID, @ChargedHours, @SESSION_ID)";
            return conn.Execute(strSQL, this) == 1;
        }
    }

    public class ParkingIn : TParkingIn
    {
        VehicleType _VType;
        public VehicleType VType { get { return _VType; } set { _VType = value; OnPropertyChanged("VType"); } }
    }

    public class ParkingOut : TParkingOut
    {
        private POutMemberDetails _MemDetails;
        private RateMaster _RateName;
        public RateMaster RateName { get { return _RateName; } set { _RateName = value; OnPropertyChanged("RateName"); } }
        public POutMemberDetails MemDetails { get { return _MemDetails; } set { _MemDetails = value; OnPropertyChanged("MemDetails"); } }


    }


    public class TPOutMemberDetails : BaseModel
    {
        private int _PID;
        private int _MemberId;
        private string _VCHRNO;
        public string VCHRNO { get { return _VCHRNO; } set { _VCHRNO = value; OnPropertyChanged("VCHRNO"); } }
        public int MemberId { get { return _MemberId; } set { _MemberId = value; OnPropertyChanged("MemberId"); } }
        public int PID { get { return _PID; } set { _PID = value; OnPropertyChanged("PID"); } }
    }

    public class POutMemberDetails : TPOutMemberDetails
    {
        private TMember _POutMember;
        public TMember POutMember { get { return _POutMember; } set { _POutMember = value; OnPropertyChanged("POutMember"); } }
    }


    public class TMember : BaseModel
    {
        private int _MemberId;
        private int _MemberName;
        public int MemberId { get { return _MemberId; } set { _MemberId = value; OnPropertyChanged("MemberId"); } }
        public int MemberName { get { return _MemberName; } set { _MemberName = value; OnPropertyChanged("MemberName"); } }
    }
}

