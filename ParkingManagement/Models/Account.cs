using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class Account
    {
        public int SERIAL { get; set; }
        public string ACID { get; set; }
        public string ACNAME { get; set; }
        public string PARENT { get; set; }
        public string TYPE { get; set; }
        public string OPBAL { get; set; }
        public string MAPID { get; set; }
        public int IsBasicAc { get; set; }
        public string ADDRESS { get; set; }
        public string PHONE { get; set; }
        public string FAX { get; set; }
        public string EMAIL { get; set; }
        public string VATNO { get; set; }
        public string PType { get; set; }
        public int CRLIMIT { get; set; }
        public byte CRPERIOD { get; set; }
        public byte SALEREF { get; set; }
        public string ACCODE { get; set; }
        public byte LEVELS { get; set; }
        public int FLGNEW { get; set; }
        public int COMMON { get; set; }
        public string ACTYPE { get; set; }
        public string CrmCode { get; set; }
        public string INITIAL { get; set; }
        public DateTime? EDATE { get; set; }
        public string DISMODE { get; set; }
        public string MCAT { get; set; }
        public byte HASSUBLEDGER { get; set; }
        public int RATETYPE { get; set; }
        public int INVCHECK { get; set; }
        public int LEADTIME { get; set; }
        public int DUELIMIT { get; set; }
        public int CURRENCY { get; set; }
        public int PRICETAG { get; set; }
        public byte ISACTIVE { get; set; }
        public string MEMID { get; set; }
    }
}
