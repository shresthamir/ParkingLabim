using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using Dapper;
namespace ParkingManagement.ViewModel
{
    class ParkingAreaViewModel : BaseViewModel
    {
        ParkingArea _PA;
        ParkingArea _SelectedPA;
        ObservableCollection<VehicleType> _VTypeList;
        private ObservableCollection<ParkingArea> _PAList;

        public ParkingArea PA { get { return _PA; } set { _PA = value; OnPropertyChanged("PA"); } }
        public ParkingArea SelectedPA { get { return _SelectedPA; } set { _SelectedPA = value; OnPropertyChanged("SelectedPA"); } }
        public ObservableCollection<VehicleType> VTypeList { get { return _VTypeList; } set { _VTypeList = value; OnPropertyChanged("VTypeList"); } }
        public ObservableCollection<ParkingArea> PAList { get { return _PAList; } set { _PAList = value; OnPropertyChanged("PAList"); } }
        public List<string> FloorList { get { return PAList.OrderBy(x => x.FLOOR).Select(x => x.FLOOR).Distinct().ToList(); } }


        public ParkingAreaViewModel()
        {
            PA = new ParkingArea();
            try
            {
                MessageBoxCaption = "Occupency Area Setup";
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {                  
                    VTypeList = new ObservableCollection<VehicleType>(Conn.Query<VehicleType>("SELECT VTYPEID, [Description], Capacity, [UID] FROM VehicleType"));
                    PAList = new ObservableCollection<ParkingArea>(Conn.Query<ParkingArea>("SELECT PA_ID, PA_NAME,[Description], Capacity, VehicleType, [FLOOR], MinVacantLot  FROM PARKINGAREA"));
                    PAList.CollectionChanged += PAList_CollectionChanged;
                }
                foreach (ParkingArea pa in PAList)
                {
                    pa.VType = VTypeList.First(x => x.VTypeID == pa.VehicleType);
                }
                LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                EditCommand = new RelayCommand(ExecuteEdit);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                DeleteCommand = new RelayCommand(ExecuteDelete);
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void PAList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("FloorList");
        }

        private bool CanExecuteSave(object obj)
        {
            return PA.Error == string.Empty;
        }

        private bool CanExecuteLoad(object obj)
        {
            return (_action != ButtonAction.New && _action != ButtonAction.Edit);
        }
        private void ExecuteNew(object obj)
        {
            PA.PA_ID = GetID();
            ExecuteUndo(null);
            SetAction(ButtonAction.New);
        }

        private void ExecuteEdit(object obj)
        {
            SetAction(ButtonAction.Edit);
        }
        private void ExecuteLoad(object obj)
        {
            if (obj != null)
            {
                short id = Convert.ToInt16(obj);
                if (!PAList.Any(x => x.PA_ID == id))
                {
                    MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedPA = PAList.FirstOrDefault(x => x.PA_ID == id);
            }
            PA = new ParkingArea
            {
                PA_ID = SelectedPA.PA_ID,
                PA_Name = SelectedPA.PA_Name,
                VehicleType = SelectedPA.VehicleType,
                Description = SelectedPA.Description,
                Capacity = SelectedPA.Capacity,
                UID = SelectedPA.UID,
                FLOOR = SelectedPA.FLOOR,
                MinVacantLot = SelectedPA.MinVacantLot
            };
            SetAction(ButtonAction.Selected);
        }
        private void ExecuteUndo(object obj)
        {
            PA = new ParkingArea();
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveParkingArea();
            else if (_action == ButtonAction.Edit)
                UpdateParkingArea();
        }

        private void SaveParkingArea()
        {
            if (MessageBox.Show("You are about to Save new Occupency Area. Do you really want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                PA.PA_ID = GetID();
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        if ((int)Conn.ExecuteScalar(string.Format("SELECT COUNT(PA_ID) FROM PARKINGAREA WHERE PA_NAME = '{0}'", PA.PA_Name), transaction: tran) > 0)
                        {
                            MessageBox.Show("Occupency Area with same name already exist. Please enter another name and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        PA.Save(tran);
                        GlobalClass.SetUserActivityLog(tran, "Occupency Area Setting", "New", WorkDetail: "PA_ID : " + PA.PA_ID, Remarks: PA.Description);
                        tran.Commit();
                    }
                    MessageBox.Show("Occupency Area Saved Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    PAList.Add(PA);
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateParkingArea()
        {
            if (MessageBox.Show("Are you sure you want to Edit this Occupency Area?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        if ((int)Conn.ExecuteScalar(string.Format("SELECT COUNT(PA_ID) FROM ParkingArea WHERE PA_NAME = '{0}' AND PA_ID <>{1}", PA.PA_Name, PA.PA_ID), transaction: tran) > 0)
                        {
                            MessageBox.Show("Occupency Area with same name already exist. Please enter another name and try again.", "Save Data", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        TParkingArea PArea = Conn.Query<TParkingArea>("SELECT * FROM ParkingArea WHERE PA_ID = @PA_ID", PA, tran).First();
                        PA.Update(tran);
                        GlobalClass.SetUserActivityLog(tran, "Occupency Area Setting", "Edit", WorkDetail: "PA_ID : " + PA.PA_ID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(PArea));
                        tran.Commit();
                    }
                    MessageBox.Show("Occupency Area Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    var pa = PAList.First(x => x.PA_ID == PA.PA_ID);
                    pa.Description = PA.Description;
                    pa.Capacity = PA.Capacity;
                    pa.PA_Name = PA.PA_Name;
                    pa.VehicleType = PA.VehicleType;
                    pa.FLOOR = PA.FLOOR;
                    pa.MinVacantLot = PA.MinVacantLot;
                    pa.VType = PA.VType;
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExecuteDelete(object obj)
        {
            if (MessageBox.Show("You are about to delete selected Occupency Area. Do you really want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        TParkingArea PArea = Conn.Query<TParkingArea>("SELECT * FROM ParkingArea WHERE PA_ID = @PA_ID", PA, tran).First();
                        PA.Delete(tran);
                        GlobalClass.SetUserActivityLog(tran, "Occupency Area Setting", "Delete", WorkDetail: "PA_ID : " + PA.PA_ID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(PArea));                        
                        tran.Commit();
                    }
                    MessageBox.Show("Occupency Area Deleted successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    PAList.Remove(PAList.First(x => x.PA_ID == PA.PA_ID));
                    ExecuteUndo(null);
                }

            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected occupency area type cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private short GetID()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    return Convert.ToByte(Conn.ExecuteScalar("SELECT ISNULL(MAX(PA_ID),0) + 1 FROM PARKINGAREA"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }
    }
}
