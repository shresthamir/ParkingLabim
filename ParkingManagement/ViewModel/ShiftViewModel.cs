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
using System.IO;
using System.Windows.Media.Imaging;
namespace ParkingManagement.ViewModel
{
    class ShiftViewModel : BaseViewModel
    {
        Shift _shift;
        Shift _SelectedShift;
        ObservableCollection<Shift> _ShiftList;

        public Shift shift { get { return _shift; } set { _shift = value; OnPropertyChanged("shift"); } }
        public Shift SelectedShift { get { return _SelectedShift; } set { _SelectedShift = value; OnPropertyChanged("SelectedShift"); } }
        public ObservableCollection<Shift> ShiftList { get { return _ShiftList; } set { _ShiftList = value; OnPropertyChanged("ShiftList"); } }
        public RelayCommand BrowseImageCommand { get; set; }
        public ShiftViewModel()
        {
            shift = new Shift();
            try
            {
                MessageBoxCaption = "Attendant Shift Setup";
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = "SELECT SHIFT_ID, SHIFT_NAME, CAST(SHIFT_START AS DATETIME) SHIFT_START, CAST(SHIFT_END AS DATETIME) SHIFT_END, SHIFT_STATUS FROM tblShift";
                    ShiftList = new ObservableCollection<Shift>(Conn.Query<Shift>(strSql));
                }
                LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                EditCommand = new RelayCommand(ExecuteEdit);
                SaveCommand = new RelayCommand(ExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                DeleteCommand = new RelayCommand(ExecuteDelete);
                SetAction(ButtonAction.Init);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteLoad(object obj)
        {
            return (_action != ButtonAction.New && shift.SHIFT_ID == 0 && string.IsNullOrEmpty(shift.SHIFT_NAME));
        }
        private void ExecuteNew(object obj)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                shift.SHIFT_ID = conn.ExecuteScalar<short>("SELECT CAST(ISNULL(MAX(SHIFT_ID), 0) + 1 AS SMALLINT) FROM tblShift");
            }
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
                byte id = Convert.ToByte(obj);
                if (!ShiftList.Any(x => x.SHIFT_ID == id))
                {
                    MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedShift = ShiftList.FirstOrDefault(x => x.SHIFT_ID == id);
            }
            shift = new Shift
            {
                SHIFT_ID = SelectedShift.SHIFT_ID,
                SHIFT_NAME = SelectedShift.SHIFT_NAME,
                SHIFT_START = SelectedShift.SHIFT_START,
                UID = SelectedShift.UID,
                SHIFT_END = SelectedShift.SHIFT_END,
                SHIFT_STATUS = SelectedShift.SHIFT_STATUS
            };
            SetAction(ButtonAction.Selected);
        }

        private void ExecuteUndo(object obj)
        {
            shift = new Shift();
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveShift();
            else if (_action == ButtonAction.Edit)
                UpdateShift();
        }

        private void SaveShift()
        {
            SqlTransaction Tran;
            if (MessageBox.Show(string.Format(SaveConfirmText, "Shift"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    Tran = Conn.BeginTransaction();

                    try
                    {
                        shift.SHIFT_ID = Conn.ExecuteScalar<byte>("SELECT CAST(ISNULL(MAX(SHIFT_ID), 0) + 1 AS SMALLINT) FROM tblShift", transaction: Tran);
                        if (shift.Save(Tran))
                        {
                            GlobalClass.SetUserActivityLog(Tran, "Attendant Shift", "New", WorkDetail: "SHIFT_ID : " + shift.SHIFT_ID, Remarks: shift.SHIFT_NAME);
                            Tran.Commit();
                            MessageBox.Show("Shift Saved Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            ShiftList.Add(new Shift { SHIFT_ID = shift.SHIFT_ID, SHIFT_NAME = shift.SHIFT_NAME, SHIFT_START = shift.SHIFT_START, UID = GlobalClass.User.UID, SHIFT_STATUS = shift.SHIFT_STATUS, SHIFT_END = shift.SHIFT_END });
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("shift Type could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            Tran.Rollback();
                        }
                    }
                    catch (SqlException ex)
                    {
                        if (Tran.Connection != null)
                            Tran.Rollback();
                        if (ex.Number == 2601)
                            MessageBox.Show("Entered shift name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                        MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateShift()
        {
            if (MessageBox.Show(string.Format(UpdateConfirmText, "Shift"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Shift>("SELECT * FROM tblShift WHERE SHIFT_ID = @SHIFT_ID", shift, tran).First());
                        if (shift.Update(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Attendant Shift", "Edit", WorkDetail: "SHIFT_ID : " + shift.SHIFT_ID, Remarks: Remarks);
                            tran.Commit();
                            MessageBox.Show("Shift Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            var vehicle = ShiftList.First(x => x.SHIFT_ID == shift.SHIFT_ID);
                            vehicle.SHIFT_NAME = shift.SHIFT_NAME;
                            vehicle.SHIFT_START = shift.SHIFT_START;
                            vehicle.SHIFT_END = shift.SHIFT_END;
                            vehicle.SHIFT_STATUS = shift.SHIFT_STATUS;
                            vehicle.UID = GlobalClass.User.UID;
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("shift Type could not be updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            tran.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is SqlException && (ex as SqlException).Number == 2601)
                    MessageBox.Show("Entered shift name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDelete(object obj)
        {
            if (MessageBox.Show(string.Format(DeleteConfirmText, "Shift"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Shift>("SELECT * FROM tblShift WHERE SHIFT_ID = @SHIFT_ID", shift, tran).First());
                        if (shift.Delete(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Attendant Shift", "Delete", WorkDetail: "SHIFT_ID : " + shift.SHIFT_ID, Remarks: Remarks);
                            tran.Commit();
                            MessageBox.Show("Shift deleted successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            ShiftList.Remove(ShiftList.First(x => x.SHIFT_ID == shift.SHIFT_ID));
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Shift Type could not be Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            tran.Rollback();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected shift cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
