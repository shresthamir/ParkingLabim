using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
namespace ParkingManagement.Models
{
    class VoucherType : BaseModel
    {
        private int _VoucherId;
        private string _VoucherName;
        byte _VehicleType;
        private decimal _Rate;
        private decimal _Value;
        private int _Validity;
        private TimeSpan _ValidStart;
        private TimeSpan _ValidEnd;
        private string _VoucherInfo;
        private bool _SkipVoucherGeneration;

        public int VoucherId { get { return _VoucherId; } set { _VoucherId = value; OnPropertyChanged("VoucherId"); } }
        public string VoucherName { get { return _VoucherName; } set { _VoucherName = value; OnPropertyChanged("VoucherName"); } }
        public byte VehicleType { get { return _VehicleType; } set { _VehicleType = value; OnPropertyChanged("VehicleType"); } }
        public decimal Rate { get { return _Rate; } set { _Rate = value; OnPropertyChanged("Rate"); } }
        public decimal Value { get { return _Value; } set { _Value = value; OnPropertyChanged("Value"); } }
        public int Validity { get { return _Validity; } set { _Validity = value; OnPropertyChanged("Validity"); } }
        public TimeSpan ValidStart { get { return _ValidStart; } set { _ValidStart = value; OnPropertyChanged("ValidStart"); } }
        public TimeSpan ValidEnd { get { return _ValidEnd; } set { _ValidEnd = value; OnPropertyChanged("ValidEnd"); } }
        public DateTime Start { get { return new DateTime().Add(ValidStart); } set { ValidStart = value.TimeOfDay; OnPropertyChanged("Start"); } }
        public DateTime End { get { return new DateTime().Add(ValidEnd); } set { ValidEnd = value.TimeOfDay; OnPropertyChanged("End"); } }
        public string VoucherInfo { get { return _VoucherInfo; } set { _VoucherInfo = value; OnPropertyChanged("VoucherInfo"); } }
        public bool SkipVoucherGeneration { get { return _SkipVoucherGeneration; } set { _SkipVoucherGeneration = value; OnPropertyChanged("Skip Voucher Generation"); } }
        public VoucherType()
        {
            //ValidStart = new TimeSpan(0, 0, 0);
            //ValidEnd = new TimeSpan(0, 0, 0);
        }

        public override bool Save(SqlTransaction tran)
        {
            string strSaveSql = "INSERT INTO VoucherTypes(VoucherId, VoucherName, VehicleType, Rate, Value, ValidStart, ValidEnd, Validity, VoucherInfo, SkipVoucherGeneration) VALUES (@VoucherId, @VoucherName, @VehicleType, @Rate, @Value, @ValidStart, @ValidEnd, @Validity, @VoucherInfo, @SkipVoucherGeneration)";
            return tran.Connection.Execute(strSaveSql, this, tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            string strUpdateSql = "UPDATE VoucherTypes SET VoucherName = @VoucherName, VehicleType = @VehicleType, Rate = @Rate, Value = @Value, ValidStart = @ValidStart, ValidEnd = @ValidEnd, Validity = @Validity, VoucherInfo = @VoucherInfo, SkipVoucherGeneration = @SkipVoucherGeneration WHERE VoucherId = @VoucherId";
            return tran.Connection.Execute(strUpdateSql, this, tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            string strDeleteSql = "DELETE FROM VoucherTypes WHERE VoucherId = @VoucherId";
            return tran.Connection.Execute(strDeleteSql, this, tran) == 1;
        }
    }

    public class Voucher : BaseModel
    {

        public int VoucherNo { get; set; }
        public string BillNo { get; set; }
        public int Sno { get; set; }
        public string VoucherName { get; set; }
        public string Barcode { get; set; }
        public int VoucherId { get; set; }
        public decimal Value { get; set; }
        public DateTime ExpDate { get; set; }
        public TimeSpan ValidStart { get; set; }
        public TimeSpan ValidEnd { get; set; }
        public DateTime ScannedTime { get; set; }
        public byte FYID { get; set; }

        public override bool Save(SqlTransaction tran)
        {
            string strSave = @"INSERT INTO ParkingVouchers (VoucherNo, BillNo, Sno, VoucherName, Barcode, VoucherId, Value, ExpDate, ValidStart, ValidEnd, FYID) 
                                    OUTPUT Inserted.VoucherNo
                                    VALUES ((SELECT ISNULL(MAX(VoucherNo),0) + 1 FROM ParkingVouchers), @BillNo, @Sno, @VoucherName, @Barcode, @VoucherId, @Value, @ExpDate, @ValidStart, @ValidEnd, @FYID)";
            VoucherNo = tran.Connection.ExecuteScalar<int>(strSave, this, tran);
            return VoucherNo > 0;
        }
    }
}
