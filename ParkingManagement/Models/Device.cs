using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string Devicename { get; set; }
        public string DeviceIp { get; set; }
        public int DevicePort { get; set; }
        public int VehicleType { get; set; }
        public int IsMemberDevice { get; set; }
    }
}
