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
    class VoucherTypeViewModel : BaseViewModel
    {
        VoucherType _VType;
        VoucherType _SelectedVType;
        ObservableCollection<VoucherType> _VTypeList;
        ObservableCollection<VehicleType> _VehicleTypeList;

        public VoucherType VType { get { return _VType; } set { _VType = value; OnPropertyChanged("VType"); } }
        public VoucherType SelectedVType { get { return _SelectedVType; } set { _SelectedVType = value; OnPropertyChanged("SelectedVType"); } }
        public ObservableCollection<VoucherType> VTypeList { get { return _VTypeList; } set { _VTypeList = value; OnPropertyChanged("VTypeList"); } }
        public ObservableCollection<VehicleType> VehicleTypeList { get { return _VehicleTypeList; } set { _VehicleTypeList = value; OnPropertyChanged("VehicleTypeList"); } }


        public VoucherTypeViewModel()
        {
            MessageBoxCaption = "Voucher Type Setup";
            VType = new VoucherType();
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = @"SELECT VoucherId, VoucherName, VehicleType, Rate, Value, Validity, ValidStart, ValidEnd, VoucherInfo, SkipVoucherGeneration FROM VoucherTypes";
                    VTypeList = new ObservableCollection<VoucherType>(Conn.Query<VoucherType>(strSql));
                    strSql = @"SELECT VTypeID, [Description] FROM VehicleType";
                    VehicleTypeList = new ObservableCollection<VehicleType>(Conn.Query<VehicleType>(strSql));
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
            return (_action != ButtonAction.New && VType.VoucherId == 0 && string.IsNullOrEmpty(VType.VoucherName));
        }

        private void ExecuteNew(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    VType.VoucherId = conn.ExecuteScalar<byte>("SELECT CAST(ISNULL(MAX(VoucherId), 0) + 1 AS TINYINT) FROM VoucherTypes");
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
                if (!VTypeList.Any(x => x.VoucherId == id))
                {
                    MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedVType = VTypeList.FirstOrDefault(x => x.VoucherId == id);
            }
            VType = new VoucherType
            {
                VoucherId = SelectedVType.VoucherId,
                VoucherName = SelectedVType.VoucherName,
                Rate = SelectedVType.Rate,
                Value = SelectedVType.Value,
                ValidStart = SelectedVType.ValidStart,
                ValidEnd = SelectedVType.ValidEnd,
                Validity = SelectedVType.Validity,
                VehicleType = SelectedVType.VehicleType,
                VoucherInfo = SelectedVType.VoucherInfo,
                SkipVoucherGeneration = SelectedVType.SkipVoucherGeneration

            };
            SetAction(ButtonAction.Selected);
        }

        private void ExecuteUndo(object obj)
        {
            VType = new VoucherType();
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveVoucherType();
            else if (_action == ButtonAction.Edit)
                UpdateVoucherType();
        }

        private void SaveVoucherType()
        {
            if (MessageBox.Show(string.Format(SaveConfirmText, "Voucher Type"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction Tran = Conn.BeginTransaction())
                    {
                        VType.VoucherId = Conn.ExecuteScalar<byte>("SELECT CAST(ISNULL(MAX(VoucherId), 0) + 1 AS TINYINT) FROM VoucherTypes", transaction: Tran);                      
                        if (VType.Save(Tran))
                        {
                            GlobalClass.SetUserActivityLog(Tran,"Voucher Type", "New", WorkDetail: "VoucherId : " + VType.VoucherId, Remarks: VType.VoucherName);
                            Tran.Commit();
                            MessageBox.Show("Voucher Type Saved Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            VTypeList.Add(new VoucherType
                            {
                                VoucherId = VType.VoucherId,
                                VoucherName = VType.VoucherName,
                                Rate = VType.Rate,
                                Value = VType.Value,
                                ValidStart = VType.ValidStart,
                                ValidEnd = VType.ValidEnd,
                                Validity = VType.Validity,
                                VoucherInfo = VType.VoucherInfo,
                                SkipVoucherGeneration = VType.SkipVoucherGeneration
                            });
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Voucher Type could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered Voucher name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateVoucherType()
        {

            if (MessageBox.Show(string.Format(UpdateConfirmText, "Voucher Type"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<VoucherType>("SELECT VoucherId, VoucherName, VehicleType, Rate, Value, Validity, ValidStart, ValidEnd, VoucherInfo, SkipVoucherGeneration FROM VoucherTypes WHERE VoucherId = @VoucherId", VType, tran).First());                        
                        if (VType.Update(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Voucher Type", "Edit", WorkDetail: "VoucherId : " + VType.VoucherId, Remarks: Remarks);
                            tran.Commit();                            
                            MessageBox.Show("Voucher Type Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            var Voucher = VTypeList.First(x => x.VoucherId == VType.VoucherId);
                            Voucher.VoucherName = VType.VoucherName;
                            Voucher.Rate = VType.Rate;
                            Voucher.Value = VType.Value;
                            Voucher.ValidStart = VType.ValidStart;
                            Voucher.ValidEnd = VType.ValidEnd;
                            Voucher.Validity = VType.Validity;
                            Voucher.VoucherInfo = VType.VoucherInfo;
                            Voucher.SkipVoucherGeneration = VType.SkipVoucherGeneration;
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Voucher Type could not be updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            tran.Rollback();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered Voucher name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
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
            if (MessageBox.Show(string.Format(DeleteConfirmText, "Voucher Type"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<VoucherType>("SELECT VoucherId, VoucherName, Rate, Value, Validity, ValidStart, ValidEnd, VoucherInfo, SkipVoucherGeneration FROM VoucherTypes WHERE VoucherId = @VoucherId", VType, tran).First());
                        if (VType.Delete(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Voucher Type", "Delete", WorkDetail: "VoucherId : " + VType.VoucherId, Remarks: Remarks);
                            tran.Commit();                            
                            MessageBox.Show("Voucher Type Deleted successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            VTypeList.Remove(VTypeList.First(x => x.VoucherId == VType.VoucherId));
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Voucher Type could not be Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected Voucher type cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
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
