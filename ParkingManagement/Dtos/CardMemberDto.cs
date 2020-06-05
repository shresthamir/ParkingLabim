using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Dtos
{
    class CardMemberDto
    {
        public int MemberId { get; set; }
        public string Barcode { get; set; }
        public string MemberName { get; set; }
        public string Address { get; set; }
        public string Mobile { get; set; }
        public int SchemeId { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
