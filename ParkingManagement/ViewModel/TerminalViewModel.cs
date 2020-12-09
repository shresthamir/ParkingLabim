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
using Dapper;
using System.Windows;
namespace ParkingManagement.ViewModel
{
    class TerminalViewModel : BaseViewModel
    {
        private Terminal _TheTerminal;
        Terminal _SelectedTerminal;
        ObservableCollection<Terminal> _TerminalList;
        public Terminal TheTerminal { get { return _TheTerminal; } set { _TheTerminal = value; OnPropertyChanged("TheTerminal"); } }

        public Terminal SelectedTerminal { get { return _SelectedTerminal; } set { _SelectedTerminal = value; OnPropertyChanged("SelectedTerminal"); } }
        public ObservableCollection<Terminal> TerminalList { get { return _TerminalList; } set { _TerminalList = value; OnPropertyChanged("TerminalList"); } }

        public TerminalViewModel()
        {
            MessageBoxCaption = "Terminal Setup";
            TheTerminal = new Terminal();
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string strSql = "SELECT  TERMINAL_CODE, TERMINAL_NAME, [STATUS], [UID] FROM TERMINALS";
                    TerminalList = new ObservableCollection<Terminal>(Conn.Query<Terminal>(strSql));
                }
                LoadData = new RelayCommand(ExecuteLoad, CanExecuteLoad);
                NewCommand = new RelayCommand(ExecuteNew);
                EditCommand = new RelayCommand(ExecuteEdit);
                SaveCommand = new RelayCommand(ExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                DeleteCommand = new RelayCommand(ExecuteDelete);
                KeyFieldEnabled = true;
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private bool CanExecuteLoad(object obj)
        {
            return (_action != ButtonAction.New && string.IsNullOrEmpty(TheTerminal.TERMINAL_NAME));
        }
        private void ExecuteNew(object obj)
        {
            SetAction(ButtonAction.New);
        }

        private void ExecuteEdit(object obj)
        {
            KeyFieldEnabled = false;
            SetAction(ButtonAction.Edit);
        }
        private void ExecuteLoad(object obj)
        {
            if (obj != null)
            {
                if (!TerminalList.Any(x => x.TERMINAL_CODE == obj.ToString()))
                {
                    MessageBox.Show("Invalid Terminal code.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                SelectedTerminal = TerminalList.FirstOrDefault(x => x.TERMINAL_CODE == obj.ToString());
            }

            TheTerminal = new Terminal
            {
                TERMINAL_CODE = SelectedTerminal.TERMINAL_CODE,
                TERMINAL_NAME = SelectedTerminal.TERMINAL_NAME,
                STATUS = SelectedTerminal.STATUS,
                UID = SelectedTerminal.UID
            };
            SetAction(ButtonAction.Selected);
        }

        private void ExecuteUndo(object obj)
        {
            KeyFieldEnabled = true;
            TheTerminal = new Terminal();
            SetAction(ButtonAction.Init);
        }
        private void ExecuteSave(object obj)
        {
            if (_action == ButtonAction.New)
                SaveTerminal();
            else if (_action == ButtonAction.Edit)
                UpdateTerminal();
        }

        private void SaveTerminal()
        {
            if (MessageBox.Show(string.Format(SaveConfirmText, MessageBoxCaption), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        if (TheTerminal.Save(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Terminal Setting", "New", WorkDetail: "TERMINAL_CODE : " + TheTerminal.TERMINAL_CODE, Remarks: TheTerminal.TERMINAL_NAME);
                            tran.Commit();
                            MessageBox.Show("Terminal Saved Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            TerminalList.Add(new Terminal { TERMINAL_NAME = TheTerminal.TERMINAL_NAME, TERMINAL_CODE = TheTerminal.TERMINAL_CODE, UID = TheTerminal.UID, STATUS = TheTerminal.STATUS });
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Terminal could not be saved. Please check entered data and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    MessageBox.Show("Entered Terminal Code already exist in the database. Enter unique Code and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message,  LabelCaption.LabelCaptions["Vehicle Type"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTerminal()
        {
            if (MessageBox.Show(string.Format(UpdateConfirmText, "Terminal"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        if ((int)Conn.ExecuteScalar("SELECT COUNT(*) FROM TERMINALS WHERE TERMINAL_NAME = @TERMINAL_NAME AND TERMINAL_CODE = @TERMINAL_CODE", TheTerminal, tran) > 0)
                        {
                            MessageBox.Show("Same Terminal Name already exist. Please enter unique Name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Terminal>("SELECT * FROM TERMINALS WHERE TERMINAL_CODE = @TERMINAL_CODE", TheTerminal.TERMINAL_CODE, tran).First());
                        TheTerminal.UID = GlobalClass.User.UID;
                        if (TheTerminal.Update(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Terminal Setting", "Edit", WorkDetail: "TERMINAL_CODE : " + TheTerminal.TERMINAL_CODE, Remarks: Remarks);
                            tran.Commit();
                            MessageBox.Show("Terminal Updated Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            var terminal = TerminalList.First(x => x.TERMINAL_CODE == TheTerminal.TERMINAL_CODE);
                            terminal.TERMINAL_NAME = TheTerminal.TERMINAL_NAME;
                            terminal.STATUS = TheTerminal.STATUS;
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Terminal could not be saved. Please check entered data and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                }
            }

            catch (SqlException ex)
            {
                if (ex.Number == 2601)
                    MessageBox.Show("Entered Terminal name already exist in the database. Enter unique name and try again", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDelete(object obj)
        {
            if (MessageBox.Show(string.Format(DeleteConfirmText, "Terminal"), MessageBoxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    Conn.Open();
                    using (SqlTransaction tran = Conn.BeginTransaction())
                    {
                        string Remarks = Newtonsoft.Json.JsonConvert.SerializeObject(Conn.Query<Terminal>("SELECT * FROM TERMINALS WHERE TERMINAL_CODE = @TERMINAL_CODE", TheTerminal.TERMINAL_CODE, tran).First());
                        if (TheTerminal.Delete(tran))
                        {
                            GlobalClass.SetUserActivityLog(tran, "Terminal Setting", "DELETE", WorkDetail: "TERMINAL_CODE : " + TheTerminal.TERMINAL_CODE, Remarks: Remarks);
                            tran.Commit();
                            MessageBox.Show("Terminal Deleted Successfully", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                            TerminalList.Remove(TerminalList.First(x => x.TERMINAL_CODE == TheTerminal.TERMINAL_CODE));
                            ExecuteUndo(null);
                        }
                        else
                        {
                            MessageBox.Show("Terminal could not be Deleted. Please check entered data and try again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    MessageBox.Show("Selected Terminal cannot be deleted because it has already been linked to another transaction.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Hand);
                MessageBox.Show(ex.Number + " : " + ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
