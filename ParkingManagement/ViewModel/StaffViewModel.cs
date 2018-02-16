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
    class StaffViewModel : BaseViewModel
    {
        Staff _staff;
        Staff _SelectedStaff;        
        private ObservableCollection<Staff> _StaffList;

        public Staff staff { get { return _staff; } set { _staff = value; OnPropertyChanged("staff"); } }
        public Staff SelectedStaff { get { return _SelectedStaff; } set { _SelectedStaff = value; OnPropertyChanged("SelectedStaff"); } }
        public ObservableCollection<Staff> StaffList { get { return _StaffList; } set { _StaffList = value; OnPropertyChanged("StaffList"); } }
        public List<string> DesignationList { get { return StaffList.OrderBy(x => x.DESIGNATION).Select(x => x.DESIGNATION).Distinct().ToList(); } }
        private bool _BarcodeEnabled;
        public bool BarcodeEnabled { get { return _BarcodeEnabled; } set { _BarcodeEnabled = value; OnPropertyChanged("BarcodeEnabled"); } }
        public StaffViewModel()
        {
            staff = new Staff();
            try
            {
                MessageBoxCaption = "Staff Registration";
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {                  
                    StaffList = new ObservableCollection<Staff>(Conn.Query<Staff>("SELECT BARCODE, FULLNAME,[ADDRESS], DESIGNATION, REMARKS, [STATUS], BCODE  FROM tblStaff"));
                    StaffList.CollectionChanged += PAList_CollectionChanged;
                }               
                LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                EditCommand = new RelayCommand(ExecuteEdit);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                DeleteCommand = new RelayCommand(ExecuteDelete);
                BarcodeEnabled = true;
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
            return staff.Error == string.Empty;
        }

        private bool CanExecuteLoad(object obj)
        {
            return (_action != ButtonAction.New && _action != ButtonAction.Edit);
        }
        private void ExecuteNew(object obj)
        {
            ExecuteUndo(null);
            SetAction(ButtonAction.New);
        }

        private void ExecuteEdit(object obj)
        {
            BarcodeEnabled = false;
            SetAction(ButtonAction.Edit);
        }
        private void ExecuteLoad(object obj)
        {
            if (obj != null)
            {
                if (!StaffList.Any(x => x.BARCODE == obj.ToString()))
                {
                    MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedStaff = StaffList.FirstOrDefault(x => x.BARCODE == obj.ToString());
            }
            staff = new Staff
            {
                BARCODE = SelectedStaff.BARCODE,
                FULLNAME = SelectedStaff.FULLNAME,
                ADDRESS = SelectedStaff.ADDRESS,
                DESIGNATION = SelectedStaff.DESIGNATION,
                REMARKS = SelectedStaff.REMARKS,
                STATUS = SelectedStaff.STATUS,
                BCODE = SelectedStaff.BCODE
            };
            SetAction(ButtonAction.Selected);
        }
        private void ExecuteUndo(object obj)
        {
            staff = new Staff();
            BarcodeEnabled = true;
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveStaff();
            else if (_action == ButtonAction.Edit)
                UpdateStaff();
        }

        private void SaveStaff()
        {
            if (MessageBox.Show("You are about to Save new Staff. Do you really want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        if ((int)Conn.ExecuteScalar(string.Format("SELECT COUNT(BARCODE) FROM tblStaff WHERE BARCODE = '{0}'", staff.BARCODE), transaction: tran) > 0)
                        {
                            MessageBox.Show("Barcode already exists. Barcode cannot be duplicate.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        staff.Save(tran);
                        GlobalClass.SetUserActivityLog(tran, "Staff Registration", "New", WorkDetail: "BARCODE : " + staff.BARCODE, Remarks: staff.FULLNAME);
                        tran.Commit();
                    }
                    MessageBox.Show("Staff Saved Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    StaffList.Add(staff);
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStaff()
        {
            if (MessageBox.Show("Are you sure you want to Edit this Staff?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Shift>("SELECT * FROM tblStaff WHERE BARCODE = @BARCODE", staff, tran).First());
                        staff.Update(tran);
                        GlobalClass.SetUserActivityLog(tran, "Staff Registration", "Edit", WorkDetail: "BARCODE : " + staff.BARCODE, Remarks: Remarks);
                        tran.Commit();
                    }
                    MessageBox.Show("Staff Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    var pa = StaffList.First(x => x.BARCODE == staff.BARCODE);
                    pa.FULLNAME = staff.FULLNAME;
                    pa.ADDRESS  = staff.ADDRESS;
                    pa.DESIGNATION = staff.DESIGNATION;
                    pa.REMARKS = staff.REMARKS;
                    pa.STATUS = staff.STATUS;
                    pa.BCODE = staff.BCODE;
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
            if (MessageBox.Show("You are about to delete selected Staff. Do you really want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Shift>("SELECT * FROM tblStaff WHERE BARCODE = @BARCODE", staff, tran).First());
                        staff.Delete(tran);
                        GlobalClass.SetUserActivityLog(tran, "Staff Registration", "Delete", WorkDetail: "BARCODE : " + staff.BARCODE, Remarks: Remarks);
                        tran.Commit();
                    }
                    MessageBox.Show("Staff Deleted successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    StaffList.Remove(StaffList.First(x => x.BARCODE == staff.BARCODE));
                    ExecuteUndo(null);
                }

            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected Staff cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
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
