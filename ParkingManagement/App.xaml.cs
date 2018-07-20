using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ParkingManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (string arg in e.Args)
            {
                switch (arg)
                {
                    case "DebugPoleDisplay":
                        Library.PoleDisplay.IsDebugMode = true;
                        break;
                    case "ShowDisplaySetting":
                        Library.PoleDisplay.ShowDisplaySetting();
                        break;
                    case "CbmsTest":
                        Library.Helpers.SyncFunctions.CbmsTest = true;
                        break;
                    case "UpdateDatabase":
                       // Library.GlobalClass.UpdateDatabase(string.Empty);
                        break;
                }
            }
            base.OnStartup(e);
        }
    }
}
