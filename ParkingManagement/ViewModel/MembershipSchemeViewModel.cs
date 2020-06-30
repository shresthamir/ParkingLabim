using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dapper;
using ParkingManagement.Services;

namespace ParkingManagement.ViewModel
{
    class MembershipSchemeViewModel : BaseViewModel
    {
        private MembershipScheme _Scheme;
        private ObservableCollection<MembershipScheme> _SchemeList;
        private MembershipScheme _SelectedScheme;

        public MembershipScheme Scheme { get { return _Scheme; } set { _Scheme = value; OnPropertyChanged("Scheme"); } }
        public MembershipScheme SelectedScheme { get { return _SelectedScheme; } set { _SelectedScheme = value; OnPropertyChanged("SelectedScheme"); } }
        public ObservableCollection<MembershipScheme> SchemeList { get { return _SchemeList; } set { _SchemeList = value; OnPropertyChanged("SchemeList"); } }


        public MembershipSchemeViewModel()
        {
            try
            {
                NewCommand = new RelayCommand(NewMethod);
                SaveCommand = new RelayCommand(SaveMethod);
                EditCommand = new RelayCommand(EditMethod);
                UndoCommand = new RelayCommand(UndoMethod);
                DeleteCommand = new RelayCommand(DeleteMethod);
                LoadData = new RelayCommand(LoadMethod);
                MessageBoxCaption = "Membership Scheme Setup";
                UndoMethod(null);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    SchemeList = new ObservableCollection<MembershipScheme>(conn.Query<MembershipScheme>("SELECT * FROM MembershipScheme"));
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
                if (MessageBox.Show("You are going to Delete this Scheme.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Scheme.Delete(tran);
                        tran.Commit();
                    }
                }
                SchemeList.Remove(SchemeList.First(x => x.SchemeId == Scheme.SchemeId));
                MessageBox.Show("Scheme Successfully Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);
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
        }

        protected void LoadMethod(object obj)
        {
            try
            {
                if (SelectedScheme != null)
                {
                    Scheme = new MembershipScheme()
                    {
                        SchemeId = SelectedScheme.SchemeId,
                        SchemeName = SelectedScheme.SchemeName,
                        ValidOnHolidays = SelectedScheme.ValidOnHolidays,
                        ValidOnWeekends = SelectedScheme.ValidOnWeekends,
                        ValidHours = SelectedScheme.ValidHours,
                        Limit = SelectedScheme.Limit,
                        Discount = SelectedScheme.Discount,
                        ValidityPeriod = SelectedScheme.ValidityPeriod,
                        Rate=SelectedScheme.Rate
                    };
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

        protected void NewMethod(object obj)
        {
            try
            {
                UndoMethod(null);
                Scheme.ValidHoursList = new ObservableCollection<ValidHour>();
                Scheme.ValidHoursList.Add(new ValidHour()
                {
                    Start = new TimeSpan(),
                    End = new TimeSpan(23, 59, 59)
                });
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    Scheme.SchemeId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(SchemeId), 0) + 1 FROM MembershipScheme");
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void SaveMethod(object obj)
        {
            if (string.IsNullOrEmpty(Scheme.SchemeName))
            {
                MessageBox.Show("Scheme Name cannot be empty. Please enter Scheme Name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (Scheme.Discount > 100)
            {
                MessageBox.Show("Discount cannot be greater than 100", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (_action == ButtonAction.New)
                Save(Scheme);
            else if (_action == ButtonAction.Edit)
                UpdateScheme(Scheme);

        }

        private async void Save(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Save this Scheme.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Scheme.SchemeId = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(SchemeId), 0) + 1 FROM MembershipScheme ", transaction: tran);
                        Scheme.Save(tran);
                        //var res = await ProductService.CreateProduct(Scheme.SchemeName);
                        //if (res.status == "ok")
                        //{
                        //    tran.Commit();
                        //}
                        //else
                        //{
                        //    throw new Exception(res.result.ToString());
                        //}
                        tran.Commit();
                    }
                }
                SchemeList.Add(new MembershipScheme()
                {
                    SchemeId = Scheme.SchemeId,
                    SchemeName = Scheme.SchemeName,
                    ValidOnHolidays = Scheme.ValidOnHolidays,
                    ValidOnWeekends = Scheme.ValidOnWeekends,
                    ValidHours = Scheme.ValidHours,
                    Limit = Scheme.Limit,
                    Discount = Scheme.Discount,
                    ValidityPeriod = Scheme.ValidityPeriod,
                    Rate=Scheme.Rate
                });
                MessageBox.Show("Scheme successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);

            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateScheme(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Update this Scheme.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Scheme.Update(tran);
                        tran.Commit();
                    }
                }
                SelectedScheme.SchemeName = Scheme.SchemeName;
                SelectedScheme.ValidOnHolidays = Scheme.ValidOnHolidays;
                SelectedScheme.ValidOnWeekends = Scheme.ValidOnWeekends;
                SelectedScheme.ValidHours = Scheme.ValidHours;
                SelectedScheme.Limit = Scheme.Limit;
                SelectedScheme.Discount = Scheme.Discount;
                SelectedScheme.ValidityPeriod = Scheme.ValidityPeriod;

                MessageBox.Show("Scheme Successfully Updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);
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
                SelectedScheme = null;
                Scheme = new MembershipScheme();
                SetAction(ButtonAction.Init);
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
