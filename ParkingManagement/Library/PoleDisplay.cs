using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
namespace ParkingManagement.Library
{
    public enum PoleDisplayType
    {
        Nodisplay = 0,
        Price = 1,
        AMOUNT = 2,
        Charge = 3,
        Change = 4,
    }
    static class PoleDisplay
    {
        public static bool IsDebugMode = false;
        static SerialPort PoleDisplayPort;
        static PoleDisplay()
        {
            PoleDisplayPort = new SerialPort();
            if (!File.Exists(GlobalClass.AppDataPath + "\\PoleDisplaySetting.dat"))
            {
                File.WriteAllText(GlobalClass.AppDataPath + "\\PoleDisplaySetting.dat", "{'PortName' : 'COM1', 'BaudRate' : 2400}");
            }
            var DisplaySetting = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(GlobalClass.AppDataPath + "\\PoleDisplaySetting.dat"));
            PoleDisplayPort.PortName = DisplaySetting.PortName;
            PoleDisplayPort.BaudRate = DisplaySetting.BaudRate;
        }

        internal static void ShowDisplaySetting()
        {
            MessageBox.Show(File.ReadAllText(GlobalClass.AppDataPath + "\\PoleDisplaySetting.dat"), "Display Setting", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void WriteToDisplay(decimal Amount, PoleDisplayType displayType = PoleDisplayType.Nodisplay)
        {
            try
            {
                if (Amount == 0)
                    displayType = PoleDisplayType.Nodisplay;
                if (CheckPortName())
                {
                    string DisplayText = ((char)27).ToString() + ((char)81).ToString() + ((char)65).ToString() + Amount.ToString("#0.00") + ((char)13).ToString();
                    PoleDisplayPort.Write(DisplayText);
                    DisplayText = ((char)27).ToString() + ((char)115).ToString() + (int)displayType;
                    PoleDisplayPort.Write(DisplayText);
                }
                PoleDisplayPort.Close();
            }
            catch (Exception ex)
            {
                if (IsDebugMode)
                    MessageBox.Show(ex.GetBaseException().Message, "Pole Display Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool CheckPortName()
        {
            if (SerialPort.GetPortNames().Any(x => x == PoleDisplayPort.PortName))
            {
                try
                {
                    PoleDisplayPort.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    if (IsDebugMode)
                        MessageBox.Show(ex.GetBaseException().Message, "Pole Display Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    PoleDisplayPort.Close();
                    return false;
                }
            }
            if (IsDebugMode)
                MessageBox.Show("Selected Port does not exists", "Pole Display Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
