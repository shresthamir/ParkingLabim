using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ParkingManagement.Library.Helpers;
using ParkingManagement.Library;
using System.Collections.ObjectModel;
using ParkingManagement.Models;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Dapper;
namespace ParkingManagement.ViewModel
{

    struct UserCategory
    {
        public string Display { get; set; }
        public char Value { get; set; }
    }
    class UserViewModel : BaseViewModel
    {
        #region members

        User _user;
        ObservableCollection<User> _UserList;
        User _SelectedUser;
        private ObservableCollection<UserRight> _UserRightList;
        private List<UserCategory> _UserCatList;
        IEnumerable<TMenu> _Menus;
        private bool _UsernameEnabled;
        #endregion

        #region Property
        public bool UsernameEnabled { get { return _UsernameEnabled; } set { _UsernameEnabled = value; OnPropertyChanged("UsernameEnabled"); } }
        public List<UserCategory> UserCatList { get { return _UserCatList; } set { _UserCatList = value; OnPropertyChanged("UserCatList"); } }
        public ObservableCollection<UserRight> UserRightList { get { return _UserRightList; } set { _UserRightList = value; OnPropertyChanged("UserRightList"); } }

        public User user { get { return _user; } set { _user = value; OnPropertyChanged("user"); } }

        public User SelectedUser { get { return _SelectedUser; } set { _SelectedUser = value; OnPropertyChanged("SelectedUser"); } }
        public ObservableCollection<User> UserList { get { if (_UserList == null)                    _UserList = new ObservableCollection<User>(); return _UserList; } set { _UserList = value; OnPropertyChanged("UserList"); } }
        #endregion

        #region construction
        public UserViewModel()
        {
            MessageBoxCaption = "User Setup";
            user = new User();
            UserCatList = new List<UserCategory>();
            UserCatList.Add(new UserCategory { Display = "Administrator", Value = 'A' });
            UserCatList.Add(new UserCategory { Display = "Operator", Value = 'O' });

            UserRightList = new ObservableCollection<UserRight>();
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    UserList = new ObservableCollection<User>(Conn.Query<User>("SELECT UID, UserName, [Password], FullName, [STATUS], UserCat, MOBILE_ACCESS, DESKTOP_ACCESS, PA_ASSIGN, PA_STATUS, PA_LOG FROM USERS"));
                    _Menus = Conn.Query<TMenu>("SELECT MID, MENUNAME, PARENT FROM MENU WHERE PARENT <> 1 AND MID <> 1");
                    LoadRights(new UserRight() { MID = 0 });

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            NewCommand = new RelayCommand(ExecuteNew);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoadData);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteEdit);
            UndoCommand = new RelayCommand(ExecuteUndo);
            SetAction(ButtonAction.Init);
        }
        #endregion

        #region Methods
        void ExecuteUndo(object param)
        {
            user = new User();
            foreach (UserRight ur in UserRightList)
                ur.Open = 0;
            SetAction(ButtonAction.Init);
        }

        void ExecuteDelete(object param)
        {
            if (MessageBox.Show(string.Format(DeleteConfirmText, "User"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                using (SqlConnection cnMain = new SqlConnection(GlobalClass.TConnectionString))
                {
                    cnMain.Open();
                    using (SqlTransaction trans = cnMain.BeginTransaction())
                    {
                        User LUser = cnMain.Query<User>("SELECT * FROM USERS WHERE UID = @UID", user, trans).First();
                        LUser.Rights = new ObservableCollection<TUserRight>(cnMain.Query<TUserRight>("SELECT * FROM UserRight WHERE UID = @UID", user, trans));
                        if (user.Delete(trans))
                        {
                            GlobalClass.SetUserActivityLog(trans, "User Setting", "Delete", WorkDetail: "UID : " + user.UID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(LUser));
                            trans.Commit();
                            //using (SqlConnection cnSys = new SqlConnection(GlobalClass.DataConnectionString))
                            //{
                            //    cnSys.Execute("DROP USER " + user.UserName);
                            //    cnSys.Execute("DROP LOGIN " + user.UserName);
                            //}
                            UserList.Remove(UserList.First(x => x.UID == user.UID));
                            MessageBox.Show("User Deleted Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("User could not be Deleted. Please check entered data and Try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected User cannot be deleted because user has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                else
                    MessageBox.Show("Error : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        void LoadRights(UserRight Parent)
        {
            foreach (TMenu m in _Menus.Where(x => x.PARENT == Parent.MID))
            {
                UserRight Right = new UserRight
                {
                    MID = m.MID,
                    Menu = m,

                };
                LoadRights(Right);

                if (Parent.MID == 0)
                {
                    UserRightList.Add(Right);
                }
                else
                {
                    Right.Parent = Parent;
                    Parent.Children.Add(Right);
                }
            }
        }

        void ExecuteLoad(object obj)
        {

            try
            {
                if (obj != null)
                {
                    int id = Convert.ToInt32(obj);
                    if (!UserList.Any(x => x.UID == id))
                    {
                        MessageBox.Show("Invalid Id.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    SelectedUser = UserList.FirstOrDefault(x => x.UID == id);
                }
                user = new User
                {
                    UID = SelectedUser.UID,
                    UserName = SelectedUser.UserName,
                    FullName = SelectedUser.FullName,
                    UserCat = SelectedUser.UserCat,
                    STATUS = SelectedUser.STATUS,
                    MOBILE_ACCESS = SelectedUser.MOBILE_ACCESS,
                    DESKTOP_ACCESS = SelectedUser.DESKTOP_ACCESS,
                    PA_ASSIGN = SelectedUser.PA_ASSIGN,
                    PA_STATUS = SelectedUser.PA_STATUS,
                    PA_LOG = SelectedUser.PA_LOG
                };
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    var rights = conn.Query<UserRight>("SELECT UID, MID, [Open] FROM UserRight WHERE UID = " + user.UID);
                    foreach (UserRight right in rights)
                    {
                        foreach (UserRight ur in UserRightList)
                        {
                            ur.SetOpen(right.MID, right.Open);
                        }
                    }
                }

                SetAction(ButtonAction.Selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ExecuteNew(object param)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    user.UID = conn.ExecuteScalar<int>("SELECT ISNULL(MAX(UID), 0) + 1  FROM USERS");
                }

                UsernameEnabled = true;
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ExecuteEdit(object param)
        {
            UsernameEnabled = false;
            SetAction(ButtonAction.Edit);
        }

        void ExecuteSave(object param)
        {
            if (_action == ButtonAction.New)
                SaveNewUser();
            else if (_action == ButtonAction.Edit)
                UpdateUser();

        }

        private bool SaveNewUser()
        {
            if (MessageBox.Show(string.Format(SaveConfirmText, "User"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return false;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction Tran = Conn.BeginTransaction())
                    {
                        user.UID = Conn.ExecuteScalar<int>("SELECT ISNULL(MAX(UID), 0) + 1 FROM USERS", transaction: Tran);
                        user.DBPassword = string.Empty;
                        user.Password = GlobalClass.GetEncryptedPWD(string.Empty, ref user._Salt);
                        user.UserCat = 'A';
                        if (user.Save(Tran))
                        {
                            Conn.Execute(string.Format("INSERT INTO UserRight([UID], [MID], [OPEN]) SELECT {0}, MID, 1 FROM MENU WHERE MID = 1 OR PARENT =1", user.UID), transaction: Tran);
                            foreach (UserRight ur in UserRightList)
                            {
                                ur.UID = user.UID;
                                if (!ur.Save(Tran))
                                {
                                    MessageBox.Show("User Right could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    Tran.Rollback();
                                    return false;
                                }
                            }
                            GlobalClass.SetUserActivityLog(Tran, "User Setting", "New", WorkDetail: "UID : " + user.UID, Remarks: user.UserName);
                            Tran.Commit();

                            //using (SqlConnection cnSys = new SqlConnection(GlobalClass.DataConnectionString))
                            //using (SqlCommand cmd = cnSys.CreateCommand())
                            //{
                            //    cnSys.Open();
                            //    cmd.CommandType = CommandType.StoredProcedure;
                            //    cmd.CommandText = "SP_CREATE_USER";
                            //    cmd.Parameters.AddWithValue("@UNAME", user.UserName);
                            //    cmd.Parameters.AddWithValue("@PWD", user.DBPassword);
                            //    cmd.ExecuteNonQuery();
                            //}

                            MessageBox.Show("User Saved Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            UserList.Add(new User
                            {
                                UID = user.UID,
                                UserName = user.UserName,
                                FullName = user.FullName,
                                UserCat = user.UserCat,
                                STATUS = user.STATUS,
                                MOBILE_ACCESS = user.MOBILE_ACCESS,
                                DESKTOP_ACCESS = user.DESKTOP_ACCESS,
                                PA_LOG = user.PA_LOG,
                                PA_STATUS = user.PA_STATUS,
                                PA_ASSIGN = user.PA_ASSIGN

                            });
                            ExecuteUndo(null);
                            return true;
                        }
                        else
                        {
                            MessageBox.Show("User could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            Tran.Rollback();
                            return false;
                        }
                    }

                }
            }
            catch (SqlException ex)
            {

                if (ex.Number == 2601)
                    MessageBox.Show("Entered Username already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private bool UpdateUser()
        {
            if (MessageBox.Show(string.Format(UpdateConfirmText, "User"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return false;
            try
            {

                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        User LUser = Conn.Query<User>("SELECT * FROM USERS WHERE UID = @UID", user,tran).First();
                        LUser.Rights = new ObservableCollection<TUserRight>(Conn.Query<TUserRight>("SELECT * FROM UserRight WHERE UID = @UID", user, tran));
                        Conn.Execute("DELETE FROM UserRight WHERE UID = " + user.UID, transaction: tran);
                        user.UserCat = 'A';
                        if (user.Update(tran))
                        {
                            Conn.Execute(string.Format("INSERT INTO UserRight([UID], [MID], [OPEN]) SELECT {0}, MID, 1 FROM MENU WHERE MID = 1 OR PARENT =1", user.UID), transaction: tran);
                            foreach (UserRight ur in UserRightList)
                            {
                                ur.UID = user.UID;
                                if (!ur.Save(tran))
                                {
                                    MessageBox.Show("User Right could not be Saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    tran.Rollback();
                                    return false;
                                }
                            }
                            GlobalClass.SetUserActivityLog(tran, "User Setting", "Edit", WorkDetail: "UID : " + user.UID, Remarks: Newtonsoft.Json.JsonConvert.SerializeObject(LUser));
                            tran.Commit();
                            MessageBox.Show("User Updated Successfully.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            var TUSer = UserList.First(x => x.UID == user.UID);
                            //TUSer.UserName = user.UserName;
                            TUSer.FullName = user.FullName;
                            //TUSer.Password = user.Password;
                            TUSer.UserCat = user.UserCat;
                            TUSer.STATUS = user.STATUS;
                            TUSer.MOBILE_ACCESS = user.MOBILE_ACCESS;
                            TUSer.DESKTOP_ACCESS = user.DESKTOP_ACCESS;
                            TUSer.PA_ASSIGN = user.PA_ASSIGN;
                            TUSer.PA_LOG = user.PA_LOG;
                            TUSer.PA_STATUS = user.PA_STATUS;
                            ExecuteUndo(null);
                            return true;
                        }
                        else
                        {
                            MessageBox.Show("User could not be updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            tran.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered UserName already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

        }



        bool CanExecuteLoadData(object param)
        {
            if (user.UserName == null && _action == ButtonAction.Init)
                return true;
            else
                return false;
        }

        bool CanExecuteSave(object param)
        {
            char[] special_Chars = { ' ', '/', '\\', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '+', '=', ':', ';', ',', '<', '>', '?', '\'', '\"', '~' };
            if (user.UserName == null)
                return false;
            if (user.UserName.Split(special_Chars).Count() > 1)
                return false;
            return true;
        }

        bool CanExecuteEdit(object param)
        {
            return _action == ButtonAction.Selected;
        }
        #endregion
    }
}
