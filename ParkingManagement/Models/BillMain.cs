using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class BillMain
    {
        public string division { get; set; }
        public string terminal { get; set; }
        public string deviceId { get; set; }
        public string trnuser { get; set; }
        public decimal tender { get; set; }
        public string trnmode { get; set; }
        public string trnac { get; set; }
        public string parac { get; set; }
        public string billto { get; set; }
        public string billtoadd { get; set; }
        public string billtotel { get; set; }
        public string billtomob { get; set; }
        public string mwarehouse { get; set; }
        public string guid { get; set; }
        public string voucherAbbName { get; set; }
        public string refBillNo { get; set; }
        public string remarks { get; set; }
        public string Orders { get; set; }
        public string ConfirmedBy { get; set; }
        public int ResId { get; set; }//to disable reservaion in ticketbill

        public ObservableCollection<Product> prodList { get; set; }
        public BillMain()
        {
            prodList = new ObservableCollection<Product>();
        }
    }
    public class Product
    {
        public string mcode { get; set; }
        public string bc { get; set; }
        public string unit { get; set; }
        public decimal quantity { get; set; }
        public decimal rate { get; set; }
        public int inddiscount { get; set; }
        public int flatdiscount { get; set; }
        public string itemdesc { get; set; }
        public string warehouse { get; set; }
    }
}
