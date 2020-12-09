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
    class VehicleTypeViewModel : BaseViewModel
    {
        VehicleType _Vehicle;
        VehicleType _SelectedVehicle;
        ObservableCollection<VehicleType> _VehicleTypeList;

        public VehicleType Vehicle { get { return _Vehicle; } set { _Vehicle = value; OnPropertyChanged("Vehicle"); } }
        public VehicleType SelectedVehicle { get { return _SelectedVehicle; } set { _SelectedVehicle = value; OnPropertyChanged("SelectedVehicle"); } }
        public ObservableCollection<VehicleType> VehicleTypeList { get { return _VehicleTypeList; } set { _VehicleTypeList = value; OnPropertyChanged("VehicleTypeList"); } }
        public RelayCommand BrowseImageCommand { get; set; }
        public VehicleTypeViewModel()
        {
            MessageBoxCaption = "Entrance Type Setup";
            Vehicle = new VehicleType();
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = @"SELECT V.VTypeID, V.[Description],ISNULL(SUM(PA.Capacity),0) Capacity, V.[UID], V.ButtonImage 
                                        FROM VehicleType V LEFT JOIN PARKINGAREA PA ON V.VTypeID = PA.VehicleType
                                        GROUP BY V.VTypeID, V.[Description], V.[UID], V.ButtonImage";
                    VehicleTypeList = new ObservableCollection<VehicleType>(Conn.Query<VehicleType>(strSql));
                }
                LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                EditCommand = new RelayCommand(ExecuteEdit);
                SaveCommand = new RelayCommand(ExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                DeleteCommand = new RelayCommand(ExecuteDelete);
                BrowseImageCommand = new RelayCommand(Browse);
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Browse(object obj)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Button Image|*.png";
            ofd.ShowDialog();
            Vehicle.ImageSource = Imaging.FileToImage(ofd.FileName);
        }

        private bool CanExecuteLoad(object obj)
        {
            return (_action != ButtonAction.New && Vehicle.VTypeID == 0 && string.IsNullOrEmpty(Vehicle.Description));
        }
        private void ExecuteNew(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Vehicle.VTypeID = conn.ExecuteScalar<byte>("SELECT CAST(ISNULL(MAX(VTypeID), 0) + 1 AS TINYINT) FROM VehicleType");
                }
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                if (!VehicleTypeList.Any(x => x.VTypeID == id))
                {
                    MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedVehicle = VehicleTypeList.FirstOrDefault(x => x.VTypeID == id);
            }
            Vehicle = new VehicleType
            {
                VTypeID = SelectedVehicle.VTypeID,
                Description = SelectedVehicle.Description,
                Capacity = SelectedVehicle.Capacity,
                UID = SelectedVehicle.UID,
                ButtonImage = SelectedVehicle.ButtonImage,
            };
            if (Vehicle.ButtonImage != null)
                Vehicle.ImageSource = Imaging.BinaryToImage(Vehicle.ButtonImage);
            SetAction(ButtonAction.Selected);
        }

        private void ExecuteUndo(object obj)
        {
            Vehicle = new VehicleType();
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveVehicleType();
            else if (_action == ButtonAction.Edit)
                UpdateVehicleType();
        }

        private void SaveVehicleType()
        {
            if (MessageBox.Show(string.Format(SaveConfirmText, "Entrance Type"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction Tran = Conn.BeginTransaction())
                    {
                        Vehicle.VTypeID = Conn.ExecuteScalar<byte>("SELECT CAST(ISNULL(MAX(VTypeID), 0) + 1 AS TINYINT) FROM VehicleType", transaction: Tran);
                        if (Vehicle.ImageSource != null)
                            Vehicle.ButtonImage = Imaging.FileToBinary(Vehicle.ImageSource.UriSource.LocalPath);
                        if (Vehicle.Save(Tran))
                        {
                            GlobalClass.SetUserActivityLog(Tran,"Entrance Type", "New", WorkDetail: "VTypeID : " + Vehicle.VTypeID, Remarks: Vehicle.Description);
                            Tran.Commit();
                            MessageBox.Show("Entrance Type Saved Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            VehicleTypeList.Add(new VehicleType { VTypeID = Vehicle.VTypeID, Description = Vehicle.Description, Capacity = Vehicle.Capacity, UID = GlobalClass.User.UID });
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Entrance Type could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered Entrance name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateVehicleType()
        {

            if (MessageBox.Show(string.Format(UpdateConfirmText,  LabelCaption.LabelCaptions["Vehicle Type"]), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<TVehicleType>("SELECT VTypeID, [Description], Capacity, [UID] FROM VehicleType WHERE VTypeID = @VTypeID", Vehicle, tran).First());
                        if (Vehicle.ImageSource != null && Vehicle.ImageSource.UriSource != null)
                            Vehicle.ButtonImage = Imaging.FileToBinary(Vehicle.ImageSource.UriSource.LocalPath);
                        if (Vehicle.Update(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Vehicle Type", "Edit", WorkDetail: "VTypeId : " + Vehicle.VTypeID, Remarks: Remarks);
                            tran.Commit();                            
                            MessageBox.Show("Entrance Type Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            var vehicle = VehicleTypeList.First(x => x.VTypeID == Vehicle.VTypeID);
                            vehicle.Description = Vehicle.Description;
                            vehicle.Capacity = Vehicle.Capacity;
                            vehicle.ButtonImage = Vehicle.ButtonImage;
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Entrance Type could not be updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            tran.Rollback();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered Entrance name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDelete(object obj)
        {
            if (MessageBox.Show(string.Format(DeleteConfirmText, "Entrance Type"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<TVehicleType>("SELECT VTypeID, [Description], Capacity, [UID] FROM VehicleType WHERE VTypeID = @VTypeID", Vehicle, tran).First());
                        if (Vehicle.Delete(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Entrance Type", "Delete", WorkDetail: "VTypeId : " + Vehicle.VTypeID, Remarks: Remarks);
                            tran.Commit();                            
                            MessageBox.Show("Entrance Type Deleted successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            VehicleTypeList.Remove(VehicleTypeList.First(x => x.VTypeID == Vehicle.VTypeID));
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Entrance Type could not be Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected entrance type cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
