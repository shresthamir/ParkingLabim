using Dapper;
using ExcelDataReader;
using Microsoft.Win32;
using ParkingManagement.Library;
using ParkingManagement.Models;
using ParkingManagement.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ParkingManagement.Forms.DataUtility
{
    /// <summary>
    /// Interaction logic for AccessControlDevice.xaml
    /// </summary>
    public partial class RegisterDailyCard : Window
    {

        public RegisterDailyCard()
        {
            InitializeComponent();
            this.DataContext = new RegisterDailyCardViewModel();
        }
    }
}
