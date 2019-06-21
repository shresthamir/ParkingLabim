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
using System.Windows.Data;
using RawPrintFunctions;
using System.Data;
using ParkingManagement.Forms.Transaction;
namespace ParkingManagement.ViewModel
{
    class vmSettlement : BaseViewModel
    {

        #region Members
        private CashSettlement _Settlement;
        private List<Terminal> _TerminalList;
        private List<User> _UserList;
        private Denomination _Deno;
        private ObservableCollection<CashSettlement> _SettlementList;
        private int _SelectedId;

        #endregion

        #region Properties
        public Denomination Deno { get { return _Deno; } set { _Deno = value; OnPropertyChanged("Deno"); } }
        public CashSettlement Settlement { get { return _Settlement; } set { _Settlement = value; OnPropertyChanged("Settlement"); } }
        public List<Terminal> TerminalList { get { return _TerminalList; } set { _TerminalList = value; OnPropertyChanged("TerminalList"); } }
        public List<User> UserList { get { return _UserList; } set { _UserList = value; OnPropertyChanged("UserList"); } }
        public ObservableCollection<CashSettlement> SettlementList { get { return _SettlementList; } set { _SettlementList = value; OnPropertyChanged("SettlementList"); } }
        public int SelectedId { get { return _SelectedId; } set { _SelectedId = value; OnPropertyChanged("SelectedId"); } }

        public Visibility TerminalVisibility
        {
            get { return (GlobalClass.SettlementMode ==0)?Visibility.Collapsed:Visibility.Visible; }
        }

        public Visibility UserVisibility
        {
            get { return (GlobalClass.SettlementMode == 1) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public RelayCommand OpenDenomination { get; set; }
        public RelayCommand PrintCommand { get; set; }

        #endregion

        public vmSettlement()
        {
            try
            {

                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {

                    //SELECT ID, TRNDATE, AMOUNT, TERMINAL, TRNUSER, TRNTIME, CUSER, POST FROM CashSettlement
                    TerminalList = conn.Query<Terminal>("SELECT TERMINAL_CODE, TERMINAL_NAME FROM TERMINALS WHERE [STATUS] = 0").ToList();
                    UserList = conn.Query<User>("SELECT [UID], UserName, FullName FROM USERS WHERE [STATUS] = 0").ToList();
                }
                Settlement = new CashSettlement() { TRNDATE = DateTime.Today, TERMINAL_CODE = GlobalClass.Terminal, SETTLER_UID = GlobalClass.User.UID, SETTLED_UID = GlobalClass.User.UID };
                Settlement.PropertyChanged += Settlement_PropertyChanged;
                Settlement.OnPropertyChanged("SETTLED_UID");
                OpenDenomination = new RelayCommand(openDenomination, canOpenDenomination);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                NewCommand = new RelayCommand(ExecuteNew);
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Settlemennt", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecutePrint(object obj)
        {
            return SelectedId > 0;
        }

        private void ExecutePrint(object obj)
        {
            try
            {
                using(SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using(SqlTransaction tran = conn.BeginTransaction())
                    {
                        PrintCashSettlement(SelectedId, conn, tran);
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        private void ExecuteNew(object obj)
        {
            if (_action == ButtonAction.Init)
            {
                SetAction(ButtonAction.New);
                Settlement.SETTLER_UID = GlobalClass.User.UID;
                Deno = new Denomination();// { TRNDATE = Settlement.TRNDATE, COUNTER = Settlement.TERMINAL, USERID = Settlement.TRNUSER };
            }
            else if (_action == ButtonAction.New)
            {
                if (Settlement.AMOUNT <= 0)
                {
                    MessageBox.Show("Settlement amount cannot be Zero of less.", "Cash Settlement", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (Deno.RTOTAL != Settlement.AMOUNT)
                {
                    MessageBox.Show("Denomination and settlement amount does not match. Enter denomination correctly and try again.", "Cash Settlement", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                try
                {
                    SqlTransaction tran;
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        conn.Open();
                        tran = conn.BeginTransaction();
                        try
                        {
                            Settlement.FYID = GlobalClass.FYID;
                            Settlement.SETTLEMENT_ID = conn.ExecuteScalar<int>("SELECT CurNo FROM tblSequence WHERE VNAME = 'SID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                            Settlement._TRNDATE = conn.ExecuteScalar<DateTime>("SELECT GETDATE()", transaction: tran);
                            Settlement.TRNTIME = Settlement.TRNDATE.ToString("hh:mm tt");
                            Settlement._TRNDATE = Settlement.TRNDATE.Date;
                            Deno.FYID = GlobalClass.FYID;
                            Deno.SETTLEMENT_ID = Settlement.SETTLEMENT_ID;
                            Settlement.OnPropertyChanged("SETTLED_UID");
                            Settlement.Save(tran);
                            Deno.Save(tran);

                            CloseSession(tran);
                            
                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = 'SID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                            PrintCashSettlement(Settlement.SETTLEMENT_ID, conn, tran);

                            tran.Commit();
                            GlobalClass.StartSession();
                            MessageBox.Show("Transaction successfully saved", "Cash Settlement", MessageBoxButton.OK, MessageBoxImage.Information);
                            Settlement = new CashSettlement { TRNDATE = DateTime.Today, SETTLED_UID = GlobalClass.User.UID, TERMINAL_CODE = GlobalClass.Terminal };
                            Settlement.PropertyChanged += Settlement_PropertyChanged;
                            Settlement.SETTLED_UID = 0;
                            SetAction(ButtonAction.Init);
                        }
                        catch (SqlException ex)
                        {
                            if (tran.Connection != null)
                                tran.Rollback();
                            MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Settlemennt", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Settlemennt", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private void CloseSession(SqlTransaction tran)
        {
            string strSql = string.Empty;
            switch (GlobalClass.SettlementMode)
            {
                case 0: // Userwise Settlement
                    strSql = string.Format("UPDATE [SESSION] SET SESSION_SETTLED = {1} WHERE UID = {0} AND ISNULL(SESSION_SETTLED, 0) = 0", Settlement.SETTLED_UID, Settlement.SETTLEMENT_ID);
                    break;
                case 1: // Terminal wise Settlement
                    strSql = string.Format("UPDATE [SESSION] SET SESSION_SETTLED = {1} WHERE TERMINAL_CODE ='{0}' AND ISNULL(SESSION_SETTLED, 0) = 0", Settlement.TERMINAL_CODE, Settlement.SETTLEMENT_ID);
                    break;
                case 2: // User And Terminal wise  Settlement
                    strSql = string.Format("UPDATE [SESSION] SET SESSION_SETTLED = {2} WHERE TERMINAL_CODE ='{0}' AND UID = {1} AND ISNULL(SESSION_SETTLED, 0) = 0", Settlement.TERMINAL_CODE, Settlement.SETTLED_UID, Settlement.SETTLEMENT_ID);
                    break;
                default:
                    strSql = string.Format("UPDATE [SESSION] SET SESSION_SETTLED = {1} WHERE UID = {0} AND ISNULL(SESSION_SETTLED, 0) = 0", Settlement.SETTLED_UID, Settlement.SETTLEMENT_ID);
                    break;
            }
            tran.Connection.Execute(strSql, transaction: tran);
        }

        void Settlement_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (e.PropertyName == "TRNDATE")
                    {
                        SettlementList = new ObservableCollection<CashSettlement>(
                            conn.Query<CashSettlement>(
                                string.Format("SELECT SETTLEMENT_ID, TRNDATE, AMOUNT, TERMINAL_CODE, SETTLED_UID, TRNTIME, SETTLER_UID, POST FROM CashSettlement WHERE TRNDATE = '{0}'", Settlement.TRNDATE.ToString("MM/dd/yyyy"))
                            ));
                        foreach (CashSettlement cs in SettlementList)
                        {
                            cs.Terminal = TerminalList.First(x => x.TERMINAL_CODE == cs.TERMINAL_CODE);
                            cs.Settled_User = UserList.First(x => x.UID == cs.SETTLED_UID);
                            cs.Settler_User = UserList.First(x => x.UID == cs.SETTLER_UID);
                        }
                        if (SettlementList.Count > 0)
                            SettlementList.Add(new CashSettlement { Settler_User = new User { UserName = "Total" }, AMOUNT = SettlementList.Sum(x => x.AMOUNT) });

                    }
                    else if (e.PropertyName == "SETTLED_UID" || e.PropertyName == "TERMINAL_CODE")
                    {
                        string strSql = string.Empty;
                        switch(GlobalClass.SettlementMode)
                        {
                            case 0: // Userwise Settlement
                                strSql = string.Format("SELECT ISNULL(SUM(GROSSAMOUNT),0) FROM ParkingSales PS JOIN [SESSION] S ON PS.SESSION_ID = S.SESSION_ID WHERE S.[UID] = {0} AND SESSION_SETTLED = 0 AND LEFT(BillNo,2) IN ('SI', 'TI')", Settlement.SETTLED_UID);
                                break;
                            case 1: // Terminal wise Settlement
                                strSql = string.Format("SELECT ISNULL(SUM(GROSSAMOUNT),0) FROM ParkingSales PS JOIN [SESSION] S ON PS.SESSION_ID = S.SESSION_ID WHERE SESSION_SETTLED = 0 AND TERMINAL_CODE = '{1}' AND LEFT(BillNo,2) IN ('SI', 'TI')", Settlement.TERMINAL_CODE);
                                break;
                            case 2: // User And Terminal wise  Settlement
                                strSql = string.Format("SELECT ISNULL(SUM(GROSSAMOUNT),0) FROM ParkingSales PS JOIN [SESSION] S ON PS.SESSION_ID = S.SESSION_ID WHERE S.[UID] = {0} AND SESSION_SETTLED = 0 AND TERMINAL_CODE = '{1}' AND LEFT(BillNo,2) IN ('SI', 'TI')", Settlement.SETTLED_UID, Settlement.TERMINAL_CODE);
                                break;
                            default:
                                strSql = string.Format("SELECT ISNULL(SUM(GROSSAMOUNT),0) FROM ParkingSales PS JOIN [SESSION] S ON PS.SESSION_ID = S.SESSION_ID WHERE S.[UID] = {0} AND SESSION_SETTLED = 0 AND LEFT(BillNo,2) IN ('SI', 'TI')", Settlement.SETTLED_UID);
                                break;
                        }
                      
                        Settlement.CollectionAmount = conn.ExecuteScalar<decimal>(strSql);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cash Settlement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool canOpenDenomination(object obj)
        {
            return _action == ButtonAction.New;
        }

        private void openDenomination(object obj)
        {
            wDenomination dinoForm = new wDenomination();
            dinoForm.DataContext = this.Deno;
            dinoForm.ShowDialog();
            this.Settlement.AMOUNT = Deno.RTOTAL;
        }

        void PrintCashSettlement(int SettlementId, SqlConnection conn, SqlTransaction tran)
        {
            decimal CollectionAmount;
            decimal SettlementAmount;
            DataTable dt;
            DataRow dr;
            //// RawPrinterHelper printer = new RawPrinterHelper();

            using (DataAccess da = new DataAccess())
            {
                dt = da.getData(string.Format(@"SELECT T.TERMINAL_NAME, U.UserName,CONVERT(VARCHAR, CS.TRNDATE, 101) TRNDATE, CS.TRNTIME, CS.AMOUNT, CS.CollectionAmount, D.*  
                        FROM CashSettlement CS JOIN DENOMINATION D ON CS.SETTLEMENT_ID = D.SETTLEMENT_ID AND CS.FYID = D.FYID
                        JOIN TERMINALS T ON CS.TERMINAL_CODE = T.TERMINAL_CODE
                        JOIN USERS U ON U.UID = CS.SETTLED_UID WHERE CS.SETTLEMENT_ID = {0} AND CS.FYID = {1}", SettlementId, GlobalClass.FYID), conn, tran);
                dr = dt.Rows[0];
            }
            string strPrint = string.Empty;
            int PrintLen = 40;
            CollectionAmount = (decimal)dr["CollectionAmount"];
            SettlementAmount = (decimal)dr["Amount"];

            strPrint += ((GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ')) + Environment.NewLine;
            strPrint += ((GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ')) + Environment.NewLine;
            strPrint += "Cash Settlement Receipt".PadLeft(30, ' ') + Environment.NewLine;
            strPrint += string.Format("S. Id : {0}    Date : {1}", SettlementId.ToString().PadRight(7, ' '), dr["TRNDATE"]) + Environment.NewLine;
            strPrint += string.Format("Cashier : {0}", dr["UserName"]) + Environment.NewLine;
            strPrint += string.Format("Collection Amount : {0}", CollectionAmount.ToString("#0.00")) + Environment.NewLine;
            strPrint += string.Format("Settled Amount : {0}", SettlementAmount.ToString("#0.00")) + Environment.NewLine;
            strPrint += string.Format("Excess/Short : {0}", (SettlementAmount - CollectionAmount).ToString("#0.00")) + Environment.NewLine;

            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += "Denomination :" + Environment.NewLine;

            foreach (DataColumn dc in dt.Columns)
            {
                if (!dr[dc.ColumnName].ToString().Equals("0"))
                {
                    switch (dc.ColumnName)
                    {
                        case "R1000":
                            strPrint += "1000".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (1000 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R500":
                            strPrint += "500".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (500 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R250":
                            strPrint += "250".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (250 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R100":
                            strPrint += "100".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (100 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R50":
                            strPrint += "50".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (50 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R25":
                            strPrint += "25".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (25 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R20":
                            strPrint += "20".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (20 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R10":
                            strPrint += "10".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (10 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R5":
                            strPrint += "5".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (5 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R2":
                            strPrint += "2".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (2 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R1":
                            strPrint += "1".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (1 * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "R05":
                            strPrint += "0.5".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (0.5m * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                        case "RIC":
                            strPrint += "IC".PadLeft(6, ' ') + " X " + dr[dc.ColumnName].ToString().PadLeft(4, ' ') + " = " + (1.6m * (decimal)dr[dc.ColumnName]).ToString("#0.00").PadLeft(10, ' ') + Environment.NewLine;
                            break;
                    }
                }
            }
                                                                                                          strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += "Total :".PadLeft(15, ' ') + SettlementAmount.ToString("#0.00") + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Print Time : {0}", dr["TRNTIME"]) + Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += "".PadRight(10, '-') + "".PadLeft(20, ' ') + "".PadRight(10, '-') + Environment.NewLine;
            strPrint += "Cashier".PadRight(10, ' ') + "".PadLeft(20, ' ') + "Received By".PadRight(10, ' ') + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += ((char)29).ToString() + ((char)86).ToString() + ((char)1).ToString();
            //new PrintHelper() { PrintData = strPrint }.Print();
            RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Receipt");
        }
    }
}
