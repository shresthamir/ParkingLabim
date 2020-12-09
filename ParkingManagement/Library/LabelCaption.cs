using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Library
{
    public class LabelCaption
    {
        static bool Parking = false;
        public static Dictionary<string, string> LabelCaptions;
        static LabelCaption()
        {
            LabelCaptions = new Dictionary<string, string>();
            if (Parking)
            {
                LabelCaptions.Add("Plate No", "Plate No");
                LabelCaptions.Add("Vehicle Type", "Entrance Type");
            }
            else
            {
                LabelCaptions.Add("Plate No", "Member No");
                LabelCaptions.Add("Vehicle Type", "Member Type");
            }
        }

    }
}
