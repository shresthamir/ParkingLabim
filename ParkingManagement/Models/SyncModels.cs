using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class BillViewModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string seller_pan { get; set; }
        public string buyer_pan { get; set; }
        public string fiscal_year { get; set; }
        public string buyer_name { get; set; }
        public string invoice_number { get; set; }
        public string invoice_date { get; set; }
        public double total_sales { get; set; }
        public double taxable_sales_vat { get; set; }
        public double vat { get; set; }
        public double excisable_amount { get; set; }
        public double exercise { get; set; }
        public double taxable_sales_hst { get; set; }
        public double hst { get; set; }
        public double amount_for_esf { get; set; }
        public double esf { get; set; }
        public double export_sales { get; set; }
        public double tax_exempted_sales { get; set; }
        public bool isrealtime { get; set; }
        public DateTime datetimeClient { get; set; }
    }
    public class BillReturnViewModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string seller_pan { get; set; }
        public string buyer_pan { get; set; }
        public string fiscal_year { get; set; }
        public string buyer_name { get; set; }
        public string ref_invoice_number { get; set; }
        public string credit_note_number { get; set; }
        public string credit_note_date { get; set; }
        public string reason_for_return { get; set; }
        public double total_sales { get; set; }
        public double taxable_sales_vat { get; set; }
        public double vat { get; set; }
        public double excisable_amount { get; set; }
        public double exercise { get; set; }
        public double taxable_sales_hst { get; set; }
        public double hst { get; set; }
        public double amount_for_esf { get; set; }
        public double esf { get; set; }
        public double export_sales { get; set; }
        public double tax_exempted_sales { get; set; }
        public bool isrealtime { get; set; }
        public DateTime datetimeClient { get; set; }
    }
    public class FunctionResponse
    {
        public string status { get; set; }
        public object result { get; set; }
        public string RefNo { get; set; }
    }
}
