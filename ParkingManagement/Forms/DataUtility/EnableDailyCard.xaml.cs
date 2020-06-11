﻿using ParkingManagement.ViewModel;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for EnableDailyCard.xaml
    /// </summary>
    public partial class EnableDailyCard : Window
    {
        public EnableDailyCard()
        {
            InitializeComponent();
            this.DataContext = new EnableDailyCardViewModel();
        }
    }
}