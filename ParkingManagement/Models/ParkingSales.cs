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
    class TParkingSales : BaseModel
    {
        private string _BillNo;
        private byte _FYID;
        private DateTime _TDate;
        private string _TMiti;
        private string _TTime;
        private string _Description;
        private string _BillTo;
        private string _BILLTOADD;
        private string _BILLTOPAN;
        private decimal _Amount;
        private decimal _Discount;
        private decimal _NonTaxable;
        private decimal _Taxable;
        private decimal _VAT;
        private decimal _GrossAmount;
        private string _RefBillNo;
        private bool _TaxInvoice;
        private string _Remarks;
        private int _UID;
        private int _SESSION_ID;
        private int _PID;
        private bool _TRNMODE = true;
        public string BillNo { get { return _BillNo; } set { _BillNo = value; } }
        public byte FYID { get { return _FYID; } set { _FYID = value; } }
        public DateTime TDate { get { return _TDate; } set { _TDate = value; } }
        public string TMiti { get { return _TMiti; } set { _TMiti = value; } }
        public string TTime { get { return _TTime; } set { _TTime = value; } }
        public string Description { get { return _Description; } set { _Description = value; } }
        public string BillTo { get { return _BillTo; } set { _BillTo = value; OnPropertyChanged("BillTo"); } }
        public string BILLTOADD { get { return _BILLTOADD; } set { _BILLTOADD = value; OnPropertyChanged("BILLTOADD"); } }
        public string BILLTOPAN { get { return _BILLTOPAN; } set { _BILLTOPAN = value; OnPropertyChanged("BILLTOPAN"); } }
        public decimal Amount { get { return _Amount; } set { _Amount = value; OnPropertyChanged("Amount"); } }
        public decimal Discount { get { return _Discount; } set { _Discount = value; OnPropertyChanged("Discount"); } }
        public decimal NonTaxable { get { return _NonTaxable; } set { _NonTaxable = value; } }
        public decimal Taxable { get { return _Taxable; } set { _Taxable = value; OnPropertyChanged("Taxable"); } }
        public decimal VAT { get { return _VAT; } set { _VAT = value; OnPropertyChanged("VAT"); } }
        public decimal GrossAmount { get { return _GrossAmount; } set { _GrossAmount = value; OnPropertyChanged("GrossAmount"); } }
        public string RefBillNo { get { return _RefBillNo; } set { _RefBillNo = value; } }
        public bool TaxInvoice { get { return _TaxInvoice; } set { _TaxInvoice = value; } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; } }
        public int UID { get { return _UID; } set { _UID = value; } }
        public int SESSION_ID { get { return _SESSION_ID; } set { _SESSION_ID = value; } }
        public int PID { get { return _PID; } set { _PID = value; } }
        public bool TRNMODE { get { return _TRNMODE; } set { _TRNMODE = value; OnPropertyChanged("TRNMODE"); } }

        public override bool Save(SqlTransaction tran)
        {
            string strSQL = string.Format
                             (
                                 @"INSERT INTO ParkingSales (BillNo, FYID, TDate, TMiti, TTime, [Description], BillTo, BILLTOADD, BILLTOPAN, Amount, Discount, NonTaxable, Taxable, VAT, GrossAmount, RefBillNo, TaxInvoice, Remarks, UID, SESSION_ID, PID, TRNMODE) 
                                    VALUES (@BillNo, @FYID, @TDate, @TMiti, @TTime, @Description, @BillTo, @BILLTOADD, @BILLTOPAN, @Amount, @Discount, @NonTaxable, @Taxable, @VAT, @GrossAmount, @RefBillNo, @TaxInvoice, @Remarks, @UID, @SESSION_ID, @PID, @TRNMODE)"
                             );
            return tran.Connection.Execute(strSQL, this, tran) == 1;
        }
    }



    class TParkingSalesDetails : BaseModel
    {
        private string _BillNo;
        private byte _FYID;
        private char _PType;
        private int _ProdId;
        private string _Description;
        private decimal _Quantity;
        private decimal _Rate;
        private decimal _Amount;
        private decimal _Discount;
        private decimal _NonTaxable;
        private decimal _Taxable;
        private decimal _VAT;
        private decimal _NetAmount;
        private string _Remarks;

        public string BillNo { get { return _BillNo; } set { _BillNo = value; } }
        public byte FYID { get { return _FYID; } set { _FYID = value; } }
        public char PType { get { return _PType; } set { _PType = value; } }
        public int ProdId { get { return _ProdId; } set { _ProdId = value; OnPropertyChanged("ProdId"); } }
        public string Description { get { return _Description; } set { _Description = value; OnPropertyChanged("Description"); } }
        public decimal Quantity { get { return _Quantity; } set { _Quantity = value; OnPropertyChanged("Quantity"); } }
        public decimal Rate { get { return _Rate; } set { _Rate = value; OnPropertyChanged("Rate"); } }
        public decimal Amount { get { return _Amount; } set { _Amount = value; OnPropertyChanged("Amount"); } }
        public decimal Discount { get { return _Discount; } set { _Discount = value; } }
        public decimal NonTaxable { get { return _NonTaxable; } set { _NonTaxable = value; } }
        public decimal Taxable { get { return _Taxable; } set { _Taxable = value; } }
        public decimal VAT { get { return _VAT; } set { _VAT = value; OnPropertyChanged("VAT"); } }
        public decimal NetAmount { get { return _NetAmount; } set { _NetAmount = value; OnPropertyChanged("NetAmount"); } }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; } }


        public override bool Save(SqlTransaction tran)
        {
            string strSQL = string.Format
                             (
                                 @"INSERT INTO ParkingSalesDetails (BillNo, FYID, PType, ProdId, [Description], Quantity, Rate, Amount, Discount, NonTaxable, Taxable, VAT, NetAmount, Remarks) 
                                    VALUES (@BillNo, @FYID, @PType, @ProdId, @Description, @Quantity, @Rate, @Amount, @Discount, @NonTaxable, @Taxable, @VAT, @NetAmount, @Remarks)"
                             );
            return tran.Connection.Execute(strSQL, this, tran) == 1;
        }
    }
}