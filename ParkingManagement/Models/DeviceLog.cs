using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class DeviceLog
    {
        public int dwTMachineNumber { get; set; }
        public int dwEMachineNumber { get; set; }
        public int dwEnrollNumberInt { get; set; }
        public int dwVerifyMode { get; set; }
        public int dwInOutMode { get; set; }
        public int dwYear { get; set; }
        public int dwMonth { get; set; }
        public int dwDay { get; set; }
        public int dwHour { get; set; }
        public int dwMinute { get; set; }
        public string DeviceIp { get; set; }
        public int DeviceId { get; set; }
    }
}
