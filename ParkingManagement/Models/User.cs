using ParkingManagement.Library.Helpers;
using ParkingManagement.Library.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections.ObjectModel;

namespace ParkingManagement.Models
{

    class User : BaseModel
    {
        #region members
        int _UserID;
        string _userName;
        string _FullName;
        char _UserCat;
        string _password;
        byte _STATUS;
        byte _MOBILE_ACCESS;
        byte _DESKTOP_ACCESS;
        public string _Salt;
        private byte _PA_LOG;
        private byte _PA_STATUS;
        private byte _PA_ASSIGN;
        private ObservableCollection<TUserRight> _Rights;
        #endregion

        #region Property
        public int UID { get { return _UserID; } set { _UserID = value; OnPropertyChanged("UID"); } }
        public string UserName { get { return _userName; } set { _userName = value; OnPropertyChanged("UserName"); } }
        public string FullName { get { return _FullName; } set { _FullName = value; OnPropertyChanged("FullName"); } }
        public char UserCat { get { return _UserCat; } set { _UserCat = value; OnPropertyChanged("UserCat"); } }
        public string Password { get { return _password; } set { _password = value; OnPropertyChanged("Password"); } }

        public byte STATUS { get { return _STATUS; } set { _STATUS = value; OnPropertyChanged("STATUS"); } }
        public byte MOBILE_ACCESS { get { return _MOBILE_ACCESS; } set { _MOBILE_ACCESS = value; OnPropertyChanged("MOBILE_ACCESS"); } }
        public byte DESKTOP_ACCESS { get { return _DESKTOP_ACCESS; } set { _DESKTOP_ACCESS = value; OnPropertyChanged("DESKTOP_ACCESS"); } }
        public string SALT { get { return _Salt; } set { _Salt = value; OnPropertyChanged("SALT"); } }
        public byte PA_ASSIGN { get { return _PA_ASSIGN; } set { _PA_ASSIGN = value; OnPropertyChanged("PA_ASSIGN"); } }
        public byte PA_STATUS { get { return _PA_STATUS; } set { _PA_STATUS = value; OnPropertyChanged("PA_STATUS"); } }
        public byte PA_LOG { get { return _PA_LOG; } set { _PA_LOG = value; OnPropertyChanged("PA_LOG"); } }        
        public ObservableCollection<TUserRight> Rights { get { return _Rights; } set { _Rights = value; OnPropertyChanged("Rights"); } }
        #endregion


        public User()
        {

        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            string strSave = "INSERT INTO USERS ([UID], UserName, FullName, UserCat, Password, [STATUS], MOBILE_ACCESS, DESKTOP_ACCESS, SALT, PA_ASSIGN, PA_STATUS, PA_LOG) VALUES (@UID, @UserName, @FullName, @UserCat, @Password, @STATUS, @MOBILE_ACCESS, @DESKTOP_ACCESS, @SALT, @PA_ASSIGN, @PA_STATUS, @PA_LOG)";
            return tran.Connection.Execute(strSave, this, tran) == 1;
        }

        public override bool Update(System.Data.SqlClient.SqlTransaction tran)
        {
            string strUpdate = "UPDATE USERS SET FullName = @FullName, UserCat = @UserCat, [STATUS] = @STATUS, MOBILE_ACCESS = @MOBILE_ACCESS, DESKTOP_ACCESS = @DESKTOP_ACCESS, PA_ASSIGN = @PA_ASSIGN, PA_STATUS = @PA_STATUS, PA_LOG= @PA_LOG WHERE [UID] = @UID";
            return tran.Connection.Execute(strUpdate, this, tran) == 1;
        }

        public override bool Delete(System.Data.SqlClient.SqlTransaction tran)
        {
            string strDelete = "DELETE FROM USERS WHERE [UID] = @UID";
            return tran.Connection.Execute(strDelete, this, tran) == 1;
        }
    }


    public class TMenu : BaseModel
    {
        private byte _DELETE;
        private byte _EDIT;
        private byte _EXPORT;
        private byte _PRINT;
        private byte _NEW;
        private string _DATACONTEXT;
        private string _SHORTCUTKEY;
        private string _FORMPATH;
        private string _IMAGEPATH;
        private int _PARENT;
        private string _MENUNAME;
        private int _MID;
        public int MID { get { return _MID; } set { _MID = value; OnPropertyChanged("MID"); } }
        public string MENUNAME { get { return _MENUNAME; } set { _MENUNAME = value; OnPropertyChanged("MENUNAME"); } }
        public int PARENT { get { return _PARENT; } set { _PARENT = value; OnPropertyChanged("PARENT"); } }
        public string IMAGEPATH { get { return _IMAGEPATH; } set { _IMAGEPATH = value; OnPropertyChanged("IMAGEPATH"); } }
        public string FORMPATH { get { return _FORMPATH; } set { _FORMPATH = value; OnPropertyChanged("FORMPATH"); } }
        public string SHORTCUTKEY { get { return _SHORTCUTKEY; } set { _SHORTCUTKEY = value; OnPropertyChanged("SHORTCUTKEY"); } }
        public string DATACONTEXT { get { return _DATACONTEXT; } set { _DATACONTEXT = value; OnPropertyChanged("DATACONTEXT"); } }
        public byte NEW { get { return _NEW; } set { _NEW = value; OnPropertyChanged("NEW"); } }
        public byte EDIT { get { return _EDIT; } set { _EDIT = value; OnPropertyChanged("EDIT"); } }
        public byte DELETE { get { return _DELETE; } set { _DELETE = value; OnPropertyChanged("DELETE"); } }
        public byte PRINT { get { return _PRINT; } set { _PRINT = value; OnPropertyChanged("PRINT"); } }
        public byte EXPORT { get { return _EXPORT; } set { _EXPORT = value; OnPropertyChanged("EXPORT"); } }
    }

    public class TUserRight : BaseModel
    {
        private int _UID;
        private int _MID;
        private byte _Open;
        public byte Open { get { return _Open; } set { _Open = value; OnPropertyChanged("Open"); } }
        public int MID { get { return _MID; } set { _MID = value; OnPropertyChanged("MID"); } }
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }
    }

    public class UserRight : TUserRight, ITreeNode
    {
        private TMenu _Menu;
        private bool _IsExpaned;
        private bool _IsSelected;
        private bool _IsMatch;
        private System.Collections.ObjectModel.ObservableCollection<ITreeNode> _Children;
        private ITreeNode _Parent;
        public TMenu Menu { get { return _Menu; } set { _Menu = value; OnPropertyChanged("Menu"); } }

        public bool IsGroup
        {
            get { return Children.Count > 0; }
        }

        public bool IsExpanded { get { return _IsExpaned; } set { _IsExpaned = value; OnPropertyChanged("IsExpanded"); } }
        public bool IsSelected { get { return _IsSelected; } set { _IsSelected = value; OnPropertyChanged("IsSelected"); } }

        public bool IsMatch { get { return _IsMatch; } set { _IsMatch = value; OnPropertyChanged("IsMatch"); } }

        public string ParentID
        {
            get { return Menu.PARENT.ToString(); }
        }

        public ITreeNode Parent { get { return _Parent; } set { _Parent = value; OnPropertyChanged("Parent"); } }
        public System.Collections.ObjectModel.ObservableCollection<ITreeNode> Children { get { return _Children; } set { _Children = value; OnPropertyChanged("Children"); } }

        public string NodeName
        {
            get { return Menu.MENUNAME; }
        }

        public string NodeID
        {
            get { return Menu.MID.ToString(); }
        }

        public UserRight()
        {
            Children = new System.Collections.ObjectModel.ObservableCollection<ITreeNode>();
            this.PropertyChanged += UserRight_PropertyChanged;
        }

        void UserRight_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Open")
            {

                if (IsGroup && Open != 1)
                {
                    foreach (UserRight ur in Children)
                        ur.Open = Open;
                }
                if (Open == 1 && Parent != null)
                    (Parent as UserRight).Open = 1;
            }
        }

        public void SetOpen(int MID, byte value)
        {

            if (this.MID == MID)
            {
                Open = value;

            }
            else
            {

                foreach (UserRight child in Children)
                {
                    child.SetOpen(MID, value);
                }
            }
        }

        public override bool Save(System.Data.SqlClient.SqlTransaction tran)
        {
            foreach (UserRight child in Children)
            {
                child.UID = this.UID;
                if (!child.Save(tran))
                {
                    return false;
                }
            }
            string strSql = string.Format("INSERT INTO UserRight(UID,MID, [Open]) VALUES ({0},{1}, {2})", UID, MID, Open);
            return tran.Connection.Execute(strSql, transaction: tran) == 1;
        }
    }
}
