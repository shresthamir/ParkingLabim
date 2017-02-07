using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Dapper;
using System.Windows.Shapes;
using System.Windows.Media;
namespace ParkingManagement.ViewModel
{
    class ParkingInViewModel : BaseViewModel
    {
        private Dispatcher d;
        delegate void update();
        DateFunction.DateConverter nepDate;
        DispatcherTimer timer;
        ParkingIn _Parking;
        ParkingIn _SelectedParkingIn;
        VehicleType _Vehicle;
        string _CurTime;
        DateTime _CurDate;
        private ObservableCollection<VehicleType> _VehicleTypeList;

        private ObservableCollection<ParkingArea> _PAOccupencyList;
        public ObservableCollection<ParkingArea> PAOccupencyList { get { return _PAOccupencyList; } set { _PAOccupencyList = value; OnPropertyChanged("PAOccupencyList"); } }
        public ParkingIn Parking { get { return _Parking; } set { _Parking = value; OnPropertyChanged("Parking"); } }
        public ParkingIn SelectedParkingIn { get { return _SelectedParkingIn; } set { _SelectedParkingIn = value; OnPropertyChanged("SelectedParkingIn"); } }
        public VehicleType Vehicle { get { return _Vehicle; } set { _Vehicle = value; OnPropertyChanged("Vehicle"); } }
        public ObservableCollection<VehicleType> VTypeList { get { return _VehicleTypeList; } set { _VehicleTypeList = value; OnPropertyChanged("VTypeList"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }

        public RelayCommand PrintCommand { get; set; }
        public RelayCommand RefreshDependencyCommand { get; set; }
        public ParkingInViewModel(Dispatcher _D)
        {
            MessageBoxCaption = "Parking Entry";
            d = _D;
            nepDate = new DateFunction.DateConverter(GlobalClass.TConnectionString);
            Parking = new ParkingIn();
            SelectedParkingIn = new ParkingIn();
            Vehicle = new VehicleType();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += timer_Tick;
            timer.Start();
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = "SELECT VTypeID, [Description],(SELECT SUM(Capacity) FROM PARKINGAREA WHERE VehicleType = VTypeID) Capacity, [UID], ButtonImage from VehicleType";
                    VTypeList = new ObservableCollection<VehicleType>(conn.Query<VehicleType>(strSql));

                    foreach (VehicleType vtype in VTypeList)
                    {
                        if (vtype.ButtonImage == null)
                            continue;
                        vtype.ImageSource = Imaging.BinaryToImage(vtype.ButtonImage);
                    }


                    PAOccupencyList = new ObservableCollection<ParkingArea>(conn.Query<ParkingArea>("SELECT PA_ID, PA_NAME, Capacity, VehicleType, MinVacantLot From ParkingArea"));
                    foreach (ParkingArea pa in PAOccupencyList)
                    {
                        var VType = VTypeList.First(x => x.VTypeID == pa.VehicleType);
                        pa.VType = VType;
                        VType.PAOccupencyList.Add(pa);
                    }

                }

                NewCommand = new RelayCommand(ExecuteNew);
                SaveCommand = new RelayCommand(ExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                RefreshDependencyCommand = new RelayCommand(RefreshDependency);
                PrintCommand = new RelayCommand(ExecutePrint);
                SetAction(ButtonAction.Init);

                SqlDependency.Start(GlobalClass.DataConnectionString);
                DoDependency();

            }
            catch (Exception ex)
            {
                // var abc = new System.Windows.Media.Geometry();



                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecutePrint(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    var pin = conn.Query<ParkingIn>(string.Format("SELECT * FROM ParkingInDetails WHERE PID = (SELECT MAX(PID) FROM ParkingInDetails WHERE FYID = {0}) AND FYID = {0}", GlobalClass.FYID)).First();
                    pin.VType = _VehicleTypeList.FirstOrDefault(x => x.VTypeID == pin.VehicleType);
                    var pslip = new ParkingSlip { PIN = pin, CompanyName = GlobalClass.CompanyName, CompanyAddress = GlobalClass.CompanyAddress };
                    pslip.Print();
                    GlobalClass.SetUserActivityLog("Parking In", "Re-Print", WorkDetail: "PID : " + pin.PID);
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Parking In", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshDependency(object obj)
        {
            SqlDependency.Start(GlobalClass.TConnectionString);
            DoDependency();
        }

        private void ExecuteUndo(object obj)
        {
            SetAction(ButtonAction.Init);
            Parking = new ParkingIn();
        }
        private void ExecuteNew(object obj)
        {
            SetAction(ButtonAction.New);
            Parking = new ParkingIn();
        }


        private void ExecuteSave(object obj)
        {

            try
            {
                if (obj is VehicleType)
                {
                    Parking.VType = obj as VehicleType;
                    Parking.VehicleType = Parking.VType.VTypeID;
                }
                Parking.InDate = CurDate;
                Parking.InTime = CurTime;
                Parking.InMiti = nepDate.CBSDate(Parking.InDate);
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        Parking.PID = Conn.ExecuteScalar<int>("SELECT CurNo FROM tblSequence WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                        Parking.Barcode = BarCode(tran);
                        if (Parking.Save(tran))
                        {
                            Conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                            GlobalClass.SetUserActivityLog(tran, "Parking In", "New", WorkDetail: "PID : " + Parking.PID);
                            var pslip = new ParkingSlip { PIN = Parking, CompanyName = GlobalClass.CompanyName, CompanyAddress = GlobalClass.CompanyAddress };
                            pslip.Print();
                            tran.Commit();

                            MessageBox.Show("Vehicle Entry Success." + Environment.NewLine + "Barcode : " + Parking.Barcode + Environment.NewLine + "In Time : " + Parking.InMiti + " " + Parking.InTime, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            ExecuteUndo(null);

                        }
                        else
                            MessageBox.Show("Vehicle Entry failed.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BarCode(SqlTransaction tran)
        {
            string BCODE;
            string result = string.Empty;
            do
            {
                BCODE = Parking.PID.ToString() + (Parking.InDate.Month + Parking.InDate.Day + Parking.InDate.Year).ToString();
                BCODE = (GParse.ToLong(BCODE) * new Random().Next(99)).ToString();
                if (BCODE.Length > 14)
                    for (int i = 0; i < BCODE.Length / 2; i++)
                        result += BCODE.Substring(i, 1);
                else result = BCODE;
                if (result.Length > 7)
                    result = result.Substring(0, 7);
            }
            while (tran.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ParkingInDetails WHERE Barcode = @Barcode AND InDate > @InDate", new { Barcode = result, InDate = Parking.InDate.Subtract(new TimeSpan(365, 0, 0, 0, 0)).ToString("MM/dd/yyyy") }, transaction: tran) > 0);
            return result;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (CurTime != DateTime.Now.ToString("hh:mm tt"))
            {
                if (DateTime.Now.Second > 5)
                    timer.Interval = new TimeSpan(0, 0, 1);
                else
                    timer.Interval = new TimeSpan(0, 1, 0);
            }
            CurTime = DateTime.Now.ToString("hh:mm tt");
            CurDate = DateTime.Today;
        }

        void DoDependency()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    conn.Open();
                    using (var command = new SqlCommand("SELECT dbo.ParkingAreaInOutlog.[TrnTime] FROM dbo.ParkingAreaInOutlog;", conn))
                    {
                        // Create a dependency and associate it with the SqlCommand.
                        SqlDependency dependency = new SqlDependency(command);

                        // Maintain the refence in a class member.

                        // Subscribe to the SqlDependency event.
                        dependency.OnChange += OnDependencyChange;

                        command.ExecuteNonQuery();
                        update u = new update(updateUI);
                        d.Invoke(u, null);
                        //Dispatcher.Invoke(u, null);

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void updateUI()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var OcList = conn.Query("SELECT VehicleType, SUM(InFlag) - SUM(OutFlag) Occupency FROM ParkingAreaInOutlog L JOIN ParkingArea PA ON L.PA_ID = PA.PA_ID GROUP BY VehicleType");
                    foreach (dynamic d in OcList)
                        VTypeList.First(x => x.VTypeID == d.VehicleType).Occupency = d.Occupency;

                    var PaOcList = conn.Query("SELECT PA_ID, SUM(InFlag) - SUM(OutFlag) Occupency FROM ParkingAreaInOutlog GROUP BY PA_ID");
                    foreach (dynamic d in PaOcList)
                        PAOccupencyList.First(x => x.PA_ID == d.PA_ID).Occupency = d.Occupency;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OnDependencyChange(object sender, SqlNotificationEventArgs e)
        {
            SqlDependency dependency = (SqlDependency)sender;
            dependency.OnChange -= OnDependencyChange;
            SqlDependency.Start(GlobalClass.TConnectionString);
            DoDependency();
        }


    }

}



