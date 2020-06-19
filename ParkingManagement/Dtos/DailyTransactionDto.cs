using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Dtos
{
    public class DailyTransactionDto
    {
        public decimal GrossAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal vat { get; set; }
        public decimal Taxable { get; set; }
        public decimal nontaxable { get; set; }
        public DateTime TDate { get; set; }
        public string TRNTIME { get; set; }
    }
}
