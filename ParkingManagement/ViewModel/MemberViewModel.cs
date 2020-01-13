using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ParkingManagement.Library;
using System.Windows;
using Microsoft.Win32;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.OleDb;

namespace ParkingManagement.ViewModel

{
    class MemberViewModel : BaseViewModel
    {
        Forms.wImportMembers wImport;
        private string _ExcelFilePath;
        private ObservableCollection<Member> _ImportMemberList;
        private Member _Member;
        private ObservableCollection<Member> _MemberList;
        private Member _SelectedMember;
        private ObservableCollection<MembershipScheme> _SchemeList;
        private int _RecordCount;
        private int _OldRecord;
        private int _NewRecord;

        public Member Member { get { return _Member; } set { _Member = value; OnPropertyChanged("Member"); } }
        public Member SelectedMember { get { return _SelectedMember; } set { _SelectedMember = value; OnPropertyChanged("SelectedMember"); } }
        public ObservableCollection<Member> MemberList { get { return _MemberList; } set { _MemberList = value; OnPropertyChanged("MemberList"); } }
        public ObservableCollection<MembershipScheme> SchemeList { get { return _SchemeList; } set { _SchemeList = value; OnPropertyChanged("SchemeList"); } }
        public ObservableCollection<Member> ImportMemberList { get { return _ImportMemberList; } set { _ImportMemberList = value; OnPropertyChanged("ImportMemberList"); } }
        public string ExcelFilePath { get { return _ExcelFilePath; } set { _ExcelFilePath = value; OnPropertyChanged("ExcelFilePath"); } }
        public int ImportCount { get { return _RecordCount; } set { _RecordCount = value; OnPropertyChanged("ImportCount"); } }
        public int NewRecords { get { return _NewRecord; } set { _NewRecord = value; OnPropertyChanged("NewRecords"); } }
        public int OldRecords { get { return _OldRecord; } set { _OldRecord = value; OnPropertyChanged("OldRecords"); } }

        public RelayCommand BrowseCommand { get { return new RelayCommand(BrowseFile); } }
        public RelayCommand FinishCommand { get { return new RelayCommand(FinishImport); } }
        public RelayCommand LoadExcelCommand { get { return new RelayCommand(LoadExcel); } }

        private void LoadExcel(object obj)
        {
            string constr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + ExcelFilePath + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES;\"";
            using (OleDbConnection conn = new OleDbConnection(constr))
            {
                conn.Open();
                ImportMemberList = new ObservableCollection<Member>(conn.Query<Member>("Select * from [Sheet1$]"));
                ImportCount = ImportMemberList.Count;
                OldRecords = ImportMemberList.Join(MemberList, x => x.MemberId, y => y.MemberId, (x, y) => new { Old = x }).Count();
                NewRecords = ImportCount - OldRecords;
            }
        }

        private void FinishImport(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to import Members from selected file. Do you want to continue?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        foreach (Member m in ImportMemberList)
                        {
                            if(!string.IsNullOrEmpty(m.Error))
                            {
                                if (MessageBox.Show(string.Format("Error On {0}({1}) : {2}. Do you want to skip this Member and continue??", m.MemberName, m.MemberId, m.Error), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                                    return;
                                else
                                    continue;
                            }
                            if (MemberList.Any(x => x.MemberId == m.MemberId))
                                m.Update(tran);
                            else
                                m.Save(tran);

                        }
                        tran.Commit();
                        MemberList = new ObservableCollection<Member>(conn.Query<Member>("SELECT * FROM Members"));
                        MessageBox.Show("Member Import completed successfully");
                        wImport.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseFile(object obj)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".xlsx";
            openFile.Filter = "(.xlsx)|*.xlsx|All Files(*.*)|*.*";

            if (openFile.ShowDialog().Value)
            {
                ExcelFilePath = openFile.FileName;
                LoadExcel(ExcelFilePath);
            }
        }

        public MemberViewModel()
        {
            try
            {
                NewCommand = new RelayCommand(NewMethod);
                SaveCommand = new RelayCommand(SaveMethod);
                EditCommand = new RelayCommand(EditMethod);
                UndoCommand = new RelayCommand(UndoMethod);
                DeleteCommand = new RelayCommand(DeleteMethod);
                LoadData = new RelayCommand(LoadMethod);
                ImportCommand = new RelayCommand(ImportMethod);
                MessageBoxCaption = "Member Registration";

                UndoMethod(null);
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    MemberList = new ObservableCollection<Member>(conn.Query<Member>("SELECT * FROM Members"));
                    SchemeList = new ObservableCollection<MembershipScheme>(conn.Query<MembershipScheme>("SELECT * FROM MembershipScheme"));
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportMethod(object obj)
        {
            ImportCount = 0;
            NewRecords = 0;
            OldRecords = 0;
            ExcelFilePath = string.Empty;
            ImportMemberList = null;
            wImport = new Forms.wImportMembers() { DataContext = this };
            wImport.Show();
        }

        protected void DeleteMethod(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Delete this Member.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Member.Delete(tran);
                        tran.Commit();
                    }
                }
                MemberList.Remove(MemberList.First(x => x.MemberId == Member.MemberId));
                MessageBox.Show("Member Successfully Deleted.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (SelectedMember != null)
                {
                    Member = new Member()
                    {
                        MemberId = SelectedMember.MemberId,
                        MemberName = SelectedMember.MemberName,
                        Address = SelectedMember.Address,
                        Mobile = SelectedMember.Mobile,
                        ActivationDate = SelectedMember.ActivationDate,
                        ExpiryDate = SelectedMember.ExpiryDate,
                        SchemeId = SelectedMember.SchemeId,
                        Barcode =SelectedMember.Barcode
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
                Member.PropertyChanged += Member_PropertyChanged;
                SetAction(ButtonAction.New);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Member_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SchemeId" && Member.SchemeId > 0)
            {
                Member.ExpiryDate = Member.ActivationDate.AddDays(SchemeList.FirstOrDefault(x => x.SchemeId == Member.SchemeId).ValidityPeriod);
            }
        }

        protected void SaveMethod(object obj)
        {
            if (!string.IsNullOrEmpty(Member.Error))
            {
                MessageBox.Show(Member.Error, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (_action == ButtonAction.New)
                Save(Member);
            else if (_action == ButtonAction.Edit)
                UpdateMember(Member);
        }

        private void Save(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Save this Member.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Member.Save(tran);
                        tran.Commit();
                    }
                }
                MemberList.Add(new Member()
                {
                    MemberId = Member.MemberId,
                    MemberName = Member.MemberName,
                    Address = Member.Address,
                    Mobile = Member.Mobile,
                    ActivationDate = Member.ActivationDate,
                    ExpiryDate = Member.ExpiryDate,
                    SchemeId = Member.SchemeId,
                    Barcode = Member.Barcode
                });
                MessageBox.Show("Member successfully saved.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                UndoMethod(null);

            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateMember(object obj)
        {
            try
            {
                if (MessageBox.Show("You are going to Update this Member.Do you want to proceed?", MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        Member.Update(tran);
                        tran.Commit();
                    }
                }
                SelectedMember.MemberName = Member.MemberName;
                SelectedMember.Address = Member.Address;
                SelectedMember.Mobile = Member.Mobile;
                SelectedMember.ActivationDate = Member.ActivationDate;
                SelectedMember.ExpiryDate = Member.ExpiryDate;
                SelectedMember.SchemeId = Member.SchemeId;
                SelectedMember.Barcode = Member.Barcode;
                MessageBox.Show("Member Successfully Updated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
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
                SelectedMember = null;
                Member = new Member();
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
