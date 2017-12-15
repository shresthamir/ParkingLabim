using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dapper;
using System.Collections.ObjectModel;

namespace ParkingManagement.ViewModel
{
    class HolidayViewModel : BaseViewModel
    {
        private ObservableCollection<Holiday> _HolidayList;
        private Holiday _Holiday;

        private DateTime _FDate;

        private DateTime _TDate;
        private bool _SingleDate = true;
        private Holiday _Holiday_Selected;

        private bool _idPanelSelected;
        private bool _AllowMultiDate;

        public Holiday SelectedHoliday { get { return _Holiday_Selected; } set { _Holiday_Selected = value; OnPropertyChanged("SelectedHoliday"); } }
        public bool SingleDate { get { return _SingleDate; } set { _SingleDate = value; OnPropertyChanged("SingleDate"); } }

        public DateTime FDate { get { return _FDate; } set { if (_FDate == value) return; _FDate = value; OnPropertyChanged("FDate");  } }
        public DateTime TDate { get { return _TDate; } set { if (_TDate == value) return; _TDate = value; OnPropertyChanged("TDate");  } }
        public ObservableCollection<Holiday> HolidayList { get { return _HolidayList; } set { _HolidayList = value; OnPropertyChanged("HolidayList"); } }
        public Holiday Holiday { get { return _Holiday; } set { _Holiday = value; OnPropertyChanged("Holiday"); } }

        public bool IdPanelSelected { get { return _idPanelSelected; } set { _idPanelSelected = value; OnPropertyChanged("IdPanelSelected"); } }
        public bool AllowMultiDate { get { return _AllowMultiDate; } set { _AllowMultiDate = value; OnPropertyChanged("AllowMultiDate"); } }


        public HolidayViewModel()
        {
            try
            {
                NewCommand = new RelayCommand(NewMethod);
                SaveCommand = new RelayCommand(SaveMethod);
                EditCommand = new RelayCommand(EditMethod);
                UndoCommand = new RelayCommand(UndoMethod);
                DeleteCommand = new RelayCommand(DeleteMethod);
                LoadData = new RelayCommand(LoadMethod);
                MessageBoxCaption = "Public Holidays Setup";
                UndoMethod(null);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    HolidayList = new ObservableCollection<Holiday>(conn.Query<Holiday>("SELECT * FROM HOLIDAY"));
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void EditMethod(object obj)
        {
            SetAction(ButtonAction.Edit);
            AllowMultiDate = false;
        }

        protected void SaveMethod(object obj)
        {
            if (string.IsNullOrEmpty(Holiday.HolidayName) || string.IsNullOrEmpty(Convert.ToString(Holiday.HolidayDate)))
            {
                MessageBox.Show("Fields Are Empty");
                return;
            }
            if (_action == ButtonAction.New)
                Save(Holiday);
            else if (_action == ButtonAction.Edit)
                UpdateHoliday(Holiday);

        }

        private void Save(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Save this  Holiday.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        do
                        {
                            Holiday.HolidayId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(HolidayId), 0) + 1 FROM Holiday ", transaction:tran);
                            Holiday.HolidayDate = FDate;
                            Holiday.Save(tran);
                            FDate = FDate.AddDays(1);
                        }
                        while (FDate <= TDate);
                        tran.Commit();
                    }
                }
                HolidayList.Add(new Holiday
                {
                    HolidayId = Holiday.HolidayId,
                    HolidayName = Holiday.HolidayName,
                    HolidayDate = Holiday.HolidayDate
                });
                MessageBox.Show("Holiday successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);

            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateHoliday(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Update this  Holiday.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Holiday.Update(tran);
                        tran.Commit();
                    }
                }
                SelectedHoliday.HolidayName = Holiday.HolidayName;
                SelectedHoliday.HolidayDate = Holiday.HolidayDate;
                MessageBox.Show("Holiday Successfully Updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void LoadMethod(object obj)
        {
            try
            {
                if (SelectedHoliday != null)
                {

                    Holiday = new Holiday()
                    {
                        HolidayId = SelectedHoliday.HolidayId,
                        HolidayName = SelectedHoliday.HolidayName,
                        HolidayDate = SelectedHoliday.HolidayDate
                    };
                    FDate = Holiday.HolidayDate;
                    SetAction(ButtonAction.Selected);
                }                
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void UndoMethod(object obj)
        {
            try
            {
                SelectedHoliday = new Holiday();
                Holiday = new Holiday();
                SingleDate = true;
                TDate = DateTime.Today;
                FDate = DateTime.Today;
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        protected void NewMethod(object obj)
        {
            try
            {
                UndoMethod(null);
                AllowMultiDate = true;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    Holiday.HolidayId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(HolidayId), 0) + 1 FROM Holiday ");
                IdPanelSelected = false;
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        protected void DeleteMethod(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Delete this  Holiday.Do you want to proceed?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Holiday.Delete(tran);
                        tran.Commit();
                    }
                }
                HolidayList.Remove(HolidayList.First(x => x.HolidayId == Holiday.HolidayId));
                MessageBox.Show("Holiday Successfully Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
