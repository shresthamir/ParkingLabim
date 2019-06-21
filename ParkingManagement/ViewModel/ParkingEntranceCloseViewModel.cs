using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace ParkingManagement.ViewModel
{
    class ParkingEntranceCloseViewModel : BaseViewModel
    {
        private DateTime _Date = DateTime.Today;
        private ObservableCollection<ParkingEntranceClose> _ParkingList;

        public DateTime Date { get { return _Date; } set { _Date = value; OnPropertyChanged("Date"); } }
        public ObservableCollection<ParkingEntranceClose> ParkingList { get { return _ParkingList; } set { _ParkingList = value; OnPropertyChanged("ParkingList"); } }
        public ParkingEntranceCloseViewModel()
        {
            LoadData = new RelayCommand(ExecuteLoad);
            SaveCommand = new RelayCommand(ExecuteSave);
        }

        private void ExecuteSave(object obj)
        {
            try
            {
                if (!ParkingList.Any(x => x.Close))
                {
                    MessageBox.Show("No entrance is checked for closing.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (ParkingList.Any(x => x.Close && string.IsNullOrEmpty(x.Remarks)))
                {
                    MessageBox.Show("Remarks must be given for closing the entrance", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        foreach (var p in ParkingList.Where(x => x.Close))
                        {
                            ParkingOut pout = new ParkingOut()
                            {
                                PID = p.PID,
                                FYID = p.FYID,
                                OutDate = p.InDate,
                                OutMiti = p.InMiti,
                                OutTime = p.InTime,
                                SESSION_ID = GlobalClass.Session,
                                UID = GlobalClass.User.UID,
                                Remarks = p.Remarks,
                                Interval = string.Empty,
                                Rate_ID = 1
                            };
                            pout.Save(tran);
                        }
                        tran.Commit();
                        MessageBox.Show("Success", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        ParkingList.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLoad(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = "SELECT PID.PID, [Description] VehicleType, InDate, InMiti, InTime, PlateNo, Barcode, PID.FYID FROM ParkingInDetails PID" + Environment.NewLine +
                                        "JOIN VehicleType VT ON PID.VehicleType = VT.VTypeID" + Environment.NewLine +
                                        "LEFT JOIN ParkingOutDetails POD ON PID.PID = POD.PID AND PID.FYID = POD.FYID" + Environment.NewLine +
                                        "WHERE PID.InDate = @Date AND POD.PID IS NULL";
                    ParkingList = new ObservableCollection<ParkingEntranceClose>(conn.Query<ParkingEntranceClose>(strSql, Date));
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
