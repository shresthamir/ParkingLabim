using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using ParkingManagement.Library;

namespace ParkingManagement.Models
{
    public class Member : BaseModel, IDataErrorInfo
    {
        private int _MemberId;
        private string _MemberName;
        private string _Address = string.Empty;
        private string _Mobile;
        private int _SchemeId;
        private DateTime _ActivationDate = DateTime.Today;
        private DateTime _ExpiryDate;
        private string _Barcode;

        public int MemberId { get { return _MemberId; } set { _MemberId = value; OnPropertyChanged("MemberId"); } }
        public string Barcode { get { return _Barcode; } set { _Barcode = value; OnPropertyChanged("Barcode"); } }
        public string MemberName { get { return _MemberName; } set { _MemberName = value; OnPropertyChanged("MemberName"); } }
        public string Address { get { return _Address; } set { _Address = value; OnPropertyChanged("Address"); } }
        public string Mobile { get { return _Mobile; } set { _Mobile = value; OnPropertyChanged("Mobile"); } }
        public int SchemeId { get { return _SchemeId; } set { _SchemeId = value; OnPropertyChanged("SchemeId"); } }
        public DateTime ActivationDate { get { return _ActivationDate; } set { _ActivationDate = value; OnPropertyChanged("ActivationDate"); } }
        public DateTime ExpiryDate { get { return _ExpiryDate; } set { _ExpiryDate = value; OnPropertyChanged("ExpiryDate"); } }
        public DailyCard DailyCard { get; set; }

        public override bool Save(SqlTransaction tran)
        {
            //return tran.Connection.Execute("INSERT INTO Members(MemberId, MemberName, Address, Mobile, SchemeId, ActivationDate, ExpiryDate, Barcode) VALUES (@MemberId, @MemberName, @Address, @Mobile, @SchemeId, @ActivationDate, @ExpiryDate, @Barcode)", this, tran) == 1;
            return tran.Connection.Execute("INSERT INTO Members(MemberId, MemberName, Address, Mobile, SchemeId,Barcode) VALUES (@MemberId, @MemberName, @Address, @Mobile, @SchemeId, @Barcode)", this, tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            //return tran.Connection.Execute("Update Members SET MemberName = @MemberName, Address = @Address, Mobile = @Mobile, SchemeId = @SchemeId, ActivationDate = @ActivationDate, ExpiryDate = @ExpiryDate, Barcode = @Barcode WHERE MemberId = @MemberId", this, tran) == 1;
            return tran.Connection.Execute("Update Members SET MemberName = @MemberName, Address = @Address, Mobile = @Mobile, SchemeId = @SchemeId, Barcode = @Barcode WHERE MemberId = @MemberId", this, tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            return tran.Connection.Execute("DELETE FROM Members WHERE MemberId = @MemberId", this, tran) == 1;
        }


        public string Error
        {
            get
            {
                //if (string.IsNullOrEmpty(MemberId))
                if (MemberId==0)
                    return "Member Id cannot be empty";
                else if (string.IsNullOrEmpty(Barcode))
                    return "Barcode cannot be empty";
                else if (!Barcode.StartsWith(GlobalClass.MemberBarcodePrefix))
                    return "Barcode must start with '"+GlobalClass.MemberBarcodePrefix+"' character";
                else if (string.IsNullOrEmpty(MemberName))
                    return "Member Name cannot be empty";
                //else if (string.IsNullOrEmpty(Mobile))
                //    return "Mobile No cannot be empty";
                //else if (ExpiryDate < ActivationDate)
                //    return "ExpiryDate cannot be earilier than Activation Date";
                else if (SchemeId == 0)
                    return "Scheme cannot be empty.";
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
                    case "MemberName":
                        if (string.IsNullOrEmpty(MemberName))
                            Result = "Member Name cannot be empty";
                        break;
                    case "MemberId":
                        //if (string.IsNullOrEmpty(MemberId))
                        if (MemberId==0)
                            Result = "Member Id cannot be empty";
                        break;
                    case "Barcode":
                        if (string.IsNullOrEmpty(Barcode))
                            Result = "Barcode cannot be empty";
                        else if (!Barcode.StartsWith("@"))
                            Result = "Barcode must start with '@' character";
                        break;
                    case "Mobile":
                        if (string.IsNullOrEmpty(Mobile))
                            Result = "Mobile No cannot be empty";
                        break;
                    //case "ExpiryDate":
                    //    if (ExpiryDate < ActivationDate)
                    //        Result = "ExpiryDate cannot be earilier than Activation Date";
                        //break;
                    case "SchemeId":
                        if (SchemeId == 0)
                            Result = "Scheme cannot be empty.";
                        break;

                }
                return Result;
            }
        }
    }

    public class MemberDiscount : BaseModel
    {
        public string BillNo { get; set; }
        public byte FYID { get; set; }
        public int PID { get; set; }
        public int MemberId { get; set; }
        public int SchemeId { get; set; }
        public decimal Interval { get; set; }
        public decimal DiscountAmount { get; set; }
        public int SkipInterval { get; set; }
        public override bool Save(SqlTransaction tran)
        {
            return tran.Connection.Execute("INSERT INTO MemberDiscountDetail(BillNo, FYID, PID, MemberId, SchemeId, Interval, DiscountAmount, SkipInterval) VALUES (@BillNo, @FYID, @PID, @MemberId, @SchemeId, @Interval, @DiscountAmount, @SkipInterval)", this, tran) == 1;
        }
    }
}
