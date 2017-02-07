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
using Dapper;
namespace ParkingManagement.ViewModel
{
    class RateViewModel : BaseViewModel
    {
        RateMaster _Rate;
        RateDetails _RateDetails;
        RateDetails _SelectedRateDetail;
        ObservableCollection<VehicleType> _VehicleTypeList;
        private ObservableCollection<Day> _Days;

        private bool _AllDays;
        public bool AllDays 
        { 
            get { return _AllDays; } 
            set 
            {
                _AllDays = value;
                OnPropertyChanged("AllDays"); 
                if(Days!=null)
                {
                    foreach(Day d in Days)
                    {
                        d.IsChecked = value;
                    }
                }
               
            } 
        }
        public RateMaster Rate
        {
            get { return _Rate; }
            set { _Rate = value; OnPropertyChanged("Rate"); }
        }
        public RateDetails RateDetails
        {
            get { return _RateDetails; }
            set { _RateDetails = value; OnPropertyChanged("RateDetails"); }
        }
        public RateDetails SelectedRateDetail
        {
            get { return _SelectedRateDetail; }
            set { _SelectedRateDetail = value; OnPropertyChanged("SelectedRateDetail"); }
        }
        public ObservableCollection<VehicleType> VehicleTypeList
        {
            get { return _VehicleTypeList; }
            set { _VehicleTypeList = value; OnPropertyChanged("VehicleTypeList"); }
        }

        public ObservableCollection<Day> Days
        {
            get
            {
                if (_Days == null)
                {
                    _Days = new ObservableCollection<Day>();
                    _Days.Add(new Day { DayId = 1, DayName = "Sunday" });
                    _Days.Add(new Day { DayId = 2, DayName = "Monday" });
                    _Days.Add(new Day { DayId = 3, DayName = "Tuesday" });
                    _Days.Add(new Day { DayId = 4, DayName = "Wednesday" });
                    _Days.Add(new Day { DayId = 5, DayName = "Thursday" });
                    _Days.Add(new Day { DayId = 6, DayName = "Friday" });
                    _Days.Add(new Day { DayId = 7, DayName = "Saturday" });                    
                }
                foreach(Day d in _Days)
                {
                    d.PropertyChanged += d_PropertyChanged;
                }
                return _Days;
            }
        }

        void d_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" && !(sender as Day).IsChecked && AllDays)
            {
                _AllDays = false;
                OnPropertyChanged("AllDays");
            }
        }


        public RelayCommand AddRateDetail { get; set; }
        public RelayCommand EditShiftCommand { get; set; }
        public RelayCommand RemoveShiftCommand { get; set; }


        public RateViewModel()
        {
            MessageBoxCaption = "Rate Setup";
            Rate = new RateMaster();
            Rate.Rates.CollectionChanged += Rates_CollectionChanged;
            RateDetails = new RateDetails();
            RateDetails.PropertyChanged += RateDetails_PropertyChanged;
            LoadData = new RelayCommand(ExecuteLoad);
            NewCommand = new RelayCommand(ExecuteNew);
            EditCommand = new RelayCommand(ExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            SaveCommand = new RelayCommand(ExecuteSave);
            UndoCommand = new RelayCommand(ExecuteUndo);
            AddRateDetail = new RelayCommand(ExecuteAdd, CanExecuteAdd);
            EditShiftCommand = new RelayCommand(ExecuteModify, CanExecuteModify);
            RemoveShiftCommand = new RelayCommand(ExecuteRemove);
            VehicleTypeList = new ObservableCollection<VehicleType>();
            LoadVehicleTypeList();
            SetAction(ButtonAction.Init);
        }

        void Rates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("VTypeEnabled");
        }

        private bool CanExecuteAdd(object obj)
        {
            if (RateDetails.VehicleType <= 0)
                return false;
            if (RateDetails.EndTime <= RateDetails.BeginTime)
                return false;
            if (RateDetails.Mode == 0)
            {
                var CurVehicleRates = Rate.Rates.Where(x => x.VehicleType == RateDetails.VehicleType && x.EndTime == GlobalClass.EndTime);
                foreach (Day d in Days.Where(x => x.IsChecked))
                {
                    if (CurVehicleRates.Any(x => x.Day == d.DayId))
                        return false;
                }
            }
            return Days.Any(x => x.IsChecked);
        }

        public bool VTypeEnabled
        {
            get
            {
                if (Days.Any(x => x.IsChecked))
                {
                    byte DayID = Days.First(x => x.IsChecked).DayId;
                    return Rate.Rates.Where(x => x.VehicleType == RateDetails.VehicleType && x.Day == DayID).Max(x => x.EndTime) == GlobalClass.EndTime && RateDetails.Mode == 0;
                }
                return true;
            }
        }

        private bool CanExecuteModify(object obj)
        {
            //if (RateDetails.VehicleType > 0 || RateDetails.BeginTime > 0 || RateDetails.EndTime > 0 || RateDetails.Rate > 0 || RateDetails.PartyRate > 0)
            //    return false;
            return RateDetails.Mode == 0;
        }
        private void ExecuteAdd(object obj)
        {
            if (RateDetails.Mode > 0)
            {
                if (RateDetails.EndTime != SelectedRateDetail.EndTime)
                {
                    List<RateDetails> RateToBeRemoved = new List<RateDetails>();
                    foreach (Day d in Days.Where(x => x.IsChecked))
                    {
                        RateToBeRemoved.AddRange(Rate.Rates.Where(x => x.VehicleType == SelectedRateDetail.VehicleType && x.Day == d.DayId && x.BeginTime >= SelectedRateDetail.BeginTime));

                    }
                    foreach (RateDetails rd in RateToBeRemoved)
                    {
                        Rate.Rates.Remove(rd);
                    }
                    RateToBeRemoved = null;
                }
                else
                {
                    foreach (Day d in Days.Where(x => x.IsChecked))
                    {
                        foreach (RateDetails rd in Rate.Rates.Where(x => x.VehicleType == SelectedRateDetail.VehicleType && x.Day == d.DayId && x.BeginTime == SelectedRateDetail.BeginTime))
                        {
                            rd.Rate = RateDetails.Rate;
                        }
                    }
                    RateDetails.BeginTime = Rate.Rates.Where(x => x.VehicleType == SelectedRateDetail.VehicleType && x.Day == SelectedRateDetail.Day).Max(x => x.EndTime).AddSeconds(1);
                    SetDefault();
                    // RateDetails.OnPropertyChanged("VehicleType");
                    return;
                }
            }
            foreach (Day d in Days.Where(x => x.IsChecked))
            {
                Rate.Rates.Add(new RateDetails
                {
                    Rate_ID = RateDetails.Rate_ID,
                    VehicleType = RateDetails.VehicleType,
                    Day = d.DayId,
                    BeginTime = RateDetails.BeginTime,
                    EndTime = RateDetails.EndTime,
                    Rate = RateDetails.Rate,
                    VType = RateDetails.VType,
                    IsFixed = RateDetails.IsFixed,
                    DayOfWeek = d
                });
            }
            RateDetails.BeginTime = RateDetails.EndTime.AddSeconds(1);
            SetDefault();

        }
        void SetDefault()
        {
            //
            RateDetails.Rate = 0;
            RateDetails.IsFixed = false;
            Rate.OnPropertyChanged("Rates");
            RateDetails.Mode = 0;
            SelectedRateDetail = null;
            RateDetails.OnPropertyChanged("VehicleType");
            OnPropertyChanged("VTypeEnabled");
        }

        private void ExecuteDelete(object obj)
        {
            if (MessageBox.Show(string.Format(DeleteConfirmText, "Rate"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {

                        conn.Execute("DELETE FROM RateDetails WHERE Rate_ID = " + Rate.Rate_ID, transaction: trans);
                        conn.Execute("DELETE FROM RateMaster WHERE Rate_ID = " + Rate.Rate_ID, transaction: trans);
                        GlobalClass.SetUserActivityLog(trans, "Rate Setting", "Delete", WorkDetail: "Rate_ID : " + Rate.Rate_ID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(Rate));
                        trans.Commit();
                    }
                    MessageBox.Show("Rate successfully Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEdit(object obj)
        {
            SetAction(ButtonAction.Edit);

        }

        private void ExecuteLoad(object obj)
        {
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {

                    Rate = Conn.Query<RateMaster>(string.Format("SELECT Rate_ID, RateDescription, IsDefault, [UID] FROM RATEMASTER WHERE Rate_ID = {0}", obj)).First();
                    if (Rate == null)
                    {
                        MessageBox.Show("Invalid Rate ID. Please select valid Rate ID", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    Rate.Rates = new ObservableCollection<RateDetails>(Conn.Query<RateDetails>(string.Format("SELECT Rate_ID, VehicleType,[Day],CAST(BeginTime AS DATETIME) BeginTime,CAST(EndTime AS DATETIME) EndTime,Rate,IsFixed FROM RateDetails WHERE Rate_ID = {0}", obj)));
                    SetAction(ButtonAction.Selected);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteModify(object obj)
        {
            if (VTypeEnabled)
            {
                foreach (Day d in Days)
                    d.IsChecked = false;
            }
            SelectedRateDetail = obj as RateDetails;
            RateDetails = new RateDetails
            {
                Rate_ID = SelectedRateDetail.Rate_ID,
                BeginTime = SelectedRateDetail.BeginTime,
                EndTime = SelectedRateDetail.EndTime,
                Rate = SelectedRateDetail.Rate,
                Mode = 1,
                IsFixed = SelectedRateDetail.IsFixed,
                VehicleType = SelectedRateDetail.VehicleType
            };
            RateDetails.PropertyChanged += RateDetails_PropertyChanged;
            Days.First(x => x.DayId == SelectedRateDetail.Day).IsChecked = true;
            OnPropertyChanged("VTypeEnabled");
        }

        void RateDetails_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VehicleType")
            {
                foreach (Day d in Days)
                    d.IsEnabled = true;
                foreach (RateDetails rd in Rate.Rates.Where(x => x.VehicleType == RateDetails.VehicleType && x.EndTime == GlobalClass.EndTime))
                {
                    Day d = Days.First(x => x.DayId == rd.Day);
                    d.IsEnabled = false;
                    d.IsChecked = false;
                }
            }
        }

        private void ExecuteNew(object obj)
        {
            try
            {
                ExecuteUndo(null);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    Rate.Rate_ID = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(RATE_ID), 0 ) + 1 FROM RATEMASTER");
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteRemove(object obj)
        {
            this.Rate.Rates.Remove(SelectedRateDetail);
        }

        private void ExecuteSave(object obj)
        {

            if (MessageBox.Show(string.Format(SaveConfirmText, "Rate"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            if (!Validate())
                return;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        if (Rate.IsDefault)
                        {
                            conn.Execute("UPDATE RateMaster SET IsDefault = 0", transaction: trans);
                        }
                        if (_action == ButtonAction.New)
                        {
                            Rate.Rate_ID = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(RATE_ID), 0 ) + 1 FROM RATEMASTER", transaction: trans);
                            Rate.Save(trans);
                            GlobalClass.SetUserActivityLog(trans, "Rate Setting", "New", WorkDetail: "Rate_ID : " + Rate.Rate_ID, Remarks: Rate.RateDescription);
                        }
                        else if (_action == ButtonAction.Edit)
                        {
                            LRateMaster RM = conn.Query<LRateMaster>("SELECT * FROM RateMaster WHERE Rate_ID = @Rate_ID", Rate, trans).First();
                            RM.Rates = new ObservableCollection<Models.TRateDetails>(conn.Query<TRateDetails>("SELECT * FROM RateDetails WHERE Rate_ID = @Rate_ID", Rate, trans));
                            conn.Execute(string.Format("DELETE FROM RateDetails WHERE Rate_ID = {0}", Rate.Rate_ID), transaction: trans);
                            Rate.Update(trans);
                            GlobalClass.SetUserActivityLog(trans, "Rate Setting", "Edit", WorkDetail: "Rate_ID : " + Rate.Rate_ID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(RM));
                        }

                        foreach (RateDetails rd in Rate.Rates)
                        {
                            rd.Rate_ID = Rate.Rate_ID;
                            rd.Save(trans);
                        }
                        trans.Commit();
                        MessageBox.Show("Rate successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        ExecuteUndo(null);
                    }
                }
            }
            catch (SqlException SqlEx)
            {
                MessageBox.Show(SqlEx.Number + " : " + SqlEx.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteUndo(object obj)
        {
            Rate = new RateMaster();
            Rate.Rates.CollectionChanged += Rates_CollectionChanged;
            OnPropertyChanged("VTypeEnabled");
            RateDetails = new RateDetails();
            RateDetails.PropertyChanged += RateDetails_PropertyChanged;
            // LoadVehicleTypeList();
            SetAction(ButtonAction.Init);
        }



        private void LoadVehicleTypeList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                VehicleTypeList = new ObservableCollection<VehicleType>(conn.Query<VehicleType>("SELECT VTypeID, [Description] FROM vehicletype"));
            }
        }

        bool Validate()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (string.IsNullOrEmpty(Rate.RateDescription))
                    {
                        MessageBox.Show("Rate name cannot be empty. Please enter rate name and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    if (_action == ButtonAction.New)
                    {
                        if (Conn.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM RateMaster WHERE RateDescription = '{0}'", Rate.RateDescription)) > 0)
                        //if (dc.RMaster.Any(x => x.RateDescription == this.Rate.RateDescription))
                        {
                            MessageBox.Show("Rate Description '" + this.Rate.RateDescription + "' already exist. Please type another description.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return false;
                        }
                    }
                    else if (_action == ButtonAction.Edit)
                    {
                        if (Conn.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM RateMaster WHERE RateDescription = '{0}' AND Rate_ID <> {1}", Rate.RateDescription, Rate.Rate_ID)) > 0)
                        //if (dc.RMaster.Any(x => x.RateDescription == this.Rate.RateDescription && x.Rate_ID != this.Rate.Rate_ID))
                        {
                            MessageBox.Show("Rate Description '" + this.Rate.RateDescription + "' already exist. Please type another description.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return false;
                        }

                        if (Conn.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM ParkingOutDetails WHERE Rate_ID = {0}", Rate.Rate_ID)) > 0)
                        //if (dc.RMaster.Any(x => x.RateDescription == this.Rate.RateDescription && x.Rate_ID != this.Rate.Rate_ID))
                        {
                            MessageBox.Show("Selected rate has been used on transaction. Thus, cannot be edited.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return false;
                        }

                    }
                    foreach (VehicleType vt in VehicleTypeList)
                    {
                        if (Rate.Rates.Count(x => x.VehicleType == vt.VTypeID) < 7)
                        {
                            MessageBox.Show("Please complete filling Rate Details for all days and all vehicle type.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }
                    if (this.Rate.Rates.Count <= 0)
                    {
                        MessageBox.Show("Please add atleast one Rate Details", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }


}
