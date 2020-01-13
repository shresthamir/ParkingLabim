using Dapper;
using Newtonsoft.Json;
using ParkingManagement.Forms.Transaction;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using RawPrintFunctions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ParkingManagement.ViewModel
{
    public class POutVMTouch : BaseViewModel
    {
        enum Focusable
        {
            Barcode = 1,
            Finish = 2
        }
        DateConverter nepDate;
        bool _PartyEnabled;
        DispatcherTimer timer;
        Window StaffBarcode;
        ParkingIn _PIN;
        ParkingOut _POUT;
        ObservableCollection<RateMaster> _RSchemes;
        string _CurTime;
        DateTime _CurDate;
        private string _InvoicePrefix;
        private string _InvoiceNo;
        private bool _TaxInvoice;
        private bool _MustIssueTaxInvoice;
        List<Voucher> Vouchers;
        List<VoucherType> VoucherTypes;
        MemberDiscount mDiscount;
        private bool _SaveWithStaffEnabled;
        private List<DiscountScheme> _DiscountList;
        private DiscountScheme _SelectedDiscount;
        private bool IsHoliday;
        private string _TrnMode = "Sales";

        public ParkingIn PIN { get { return _PIN; } set { _PIN = value; OnPropertyChanged("PIN"); } }
        public ParkingOut POUT { get { return _POUT; } set { _POUT = value; OnPropertyChanged("POUT"); } }
        public ObservableCollection<RateMaster> RSchemes
        {
            get { return _RSchemes; }
            set { _RSchemes = value; OnPropertyChanged("RSchemes"); }
        }

        public bool TaxInvoice
        {
            get { return _TaxInvoice; }
            set
            {
                _TaxInvoice = value;
                OnPropertyChanged("TaxInvoice");
                InvoicePrefix = (value) ? "TI" : "SI";
                if (_action != ButtonAction.RePrint)
                {
                    InvoiceNo = GlobalClass.GetInvoiceNo(InvoicePrefix);
                }
            }
        }
        public bool SaveWithStaffEnabled { get { return _SaveWithStaffEnabled; } set { _SaveWithStaffEnabled = value; OnPropertyChanged("SaveWithStaffEnabled"); } }
        public bool IsEntryMode { get { return _action == ButtonAction.Init || _action == ButtonAction.Selected; } }
        public bool CanChangeInvoiceType { get { return _MustIssueTaxInvoice; } set { _MustIssueTaxInvoice = value; OnPropertyChanged("CanChangeInvoiceType"); } }
        public string TrnMode { get { return _TrnMode; } set { _TrnMode = value; OnPropertyChanged("TrnMode"); } }
        public string InvoiceNo { get { return _InvoiceNo; } set { _InvoiceNo = value; OnPropertyChanged("InvoiceNo"); } }
        public string InvoicePrefix { get { return _InvoicePrefix; } set { _InvoicePrefix = value; OnPropertyChanged("InvoicePrefix"); } }
        public string CurTime { get { return _CurTime; } set { _CurTime = value; OnPropertyChanged("CurTime"); } }
        public DateTime CurDate { get { return _CurDate; } set { _CurDate = value; OnPropertyChanged("CurDate"); } }
        public bool PartyEnabled { get { return _PartyEnabled; } set { _PartyEnabled = value; OnPropertyChanged("PartyEnabled"); } }
        private List<DiscountScheme> DiscountList { get { return _DiscountList; } set { _DiscountList = value; OnPropertyChanged("DiscountList"); } }
        public RelayCommand OpenStaffBarcodeCommand { get; set; }
        public RelayCommand SaveWithStaffCommand { get; set; }
        public RelayCommand SaveWithPrepaidCommand { get { return new RelayCommand(SaveWithPrepaid); } }

        public RelayCommand RePrintCommand { get { return new RelayCommand(ExecuteRePrint, CanExecuteRePrint); } }
        public RelayCommand LoadInvoice { get { return new RelayCommand(ExecuteLoadInvoice, CanLoadInvoice); } }
        private DiscountScheme SelectedDiscount
        {
            get { return _SelectedDiscount; }
            set
            {
                try
                {
                    if (value != null)
                    {
                        if (value.MinHrs > POUT.ChargedHours || value.MaxHrs < POUT.ChargedHours)
                        {
                            MessageBox.Show("Parking period does not meet Scheme Criteria", "Parking Out", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        else if (!value.ValidOnHolidays && IsHoliday)
                        {
                            MessageBox.Show("The scheme is not valid on Public Holidays", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        else if (!value.ValidOnWeekends && POUT.OutDate.DayOfWeek == DayOfWeek.Saturday)
                        {
                            MessageBox.Show("The scheme is not valid on Weekends", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        if (value.DiscountPercent > 0)
                        {
                            POUT.RoyaltyAmount = POUT.ChargedAmount * value.DiscountPercent / 100;
                            POUT.CashAmount = POUT.ChargedAmount - POUT.RoyaltyAmount;
                        }
                        else if (value.DisAmountList != null)
                        {
                            POUT.CashAmount = POUT.ChargedAmount - value.DisAmountList.First(x => x.VTypeID == PIN.VehicleType).Amount;
                        }
                    }
                    _SelectedDiscount = value;
                    OnPropertyChanged("SelectedDiscount");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

            }
        }

        private void ExecuteLoadInvoice(object obj)
        {
            string BillNo = InvoicePrefix + obj.ToString();
            int PID;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    PID = conn.ExecuteScalar<int>("SELECT PID FROM ParkingSales WHERE BillNo = @BillNo AND FYID = @FYID", new { BillNo = BillNo, FYID = GlobalClass.FYID });
                    //if(PID == 0)
                    //{
                    //    return;
                    //}
                    PIN = conn.Query<ParkingIn>("SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode FROM ParkingInDetails WHERE PID = @PID AND FYID = @FYID", new { PID = PID, FYID = GlobalClass.FYID }).First();
                    PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                    POUT = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID)).First();
                    SetAction(ButtonAction.InvoiceLoaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanLoadInvoice(object obj)
        {
            return _action == ButtonAction.RePrint;
        }
        private bool CanExecuteRePrint(object obj)
        {
            return _action == ButtonAction.Init;
        }

        private void ExecuteRePrint(object obj)
        {
            SetAction(ButtonAction.RePrint);
            OnPropertyChanged("IsEntryMode");
        }
        public POutVMTouch()
        {
            try
            {
                MessageBoxCaption = "Exit";
                TaxInvoice = false;
                CanChangeInvoiceType = true;
                nepDate = new DateConverter(GlobalClass.TConnectionString);
                PIN = new ParkingIn();
                POUT = new ParkingOut();
                RSchemes = new ObservableCollection<RateMaster>();

                CurDate = DateTime.Today;
                CurTime = DateTime.Now.ToString("hh:mm tt");
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Tick += timer_Tick;
                timer.Start();
                LoadRateSchemes();
                Vouchers = new List<Voucher>();
                LoadData = new RelayCommand(ExecuteLoad);
                SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
                UndoCommand = new RelayCommand(ExecuteUndo);
                PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
                OpenStaffBarcodeCommand = new RelayCommand(OpenStaffBarcode, CanExecuteSave);
                SaveWithStaffCommand = new RelayCommand(SaveWithStaff);
                POUT.PropertyChanged += POUT_PropertyChanged;
                SetAction(ButtonAction.Init);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecutePrint(object obj)
        {
            return _action == ButtonAction.InvoiceLoaded;
        }

        private void SaveWithStaff(object obj)
        {
            try
            {
                if (obj == null)
                {
                    MessageBox.Show("Invalid Barcode");
                }
                POUT.SESSION_ID = GlobalClass.Session;
                POUT.STAFF_BARCODE = obj.ToString();
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (POUT.STAFF_BARCODE != "STAMP")
                    {
                        POUT.STAFF_BARCODE = conn.ExecuteScalar<string>("SELECT BARCODE FROM tblStaff WHERE STATUS = 0 AND BCODE = '" + POUT.STAFF_BARCODE + "'");
                        if (string.IsNullOrEmpty(POUT.STAFF_BARCODE))
                        {
                            MessageBox.Show("Invalid Barcode. Please Try Again.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                        if (GlobalClass.AllowMultiVehicleForStaff == 0)
                        {
                            if (conn.ExecuteScalar<int>
                            (
                                string.Format
                                (
                                    @"SELECT COUNT(*) FROM
                            (
                                SELECT  INDATE + CAST(INTIME AS TIME) INTIME, OUTDATE + CAST(OUTTIME AS TIME) OUTTIME FROM ParkingInDetails PID 
                                JOIN ParkingOutDetails POD ON PID.PID =POD.PID AND PID.FYID = POD.FYID WHERE POD.STAFF_BARCODE = '{0}'
                            ) A WHERE (INTIME < '{1}' AND OUTTIME > '{1}') OR (INTIME > '{1}' AND OUTTIME > '{1}')", POUT.STAFF_BARCODE, PIN.InDate.Add(DateTime.Parse(PIN.InTime).TimeOfDay)
                                )
                            ) > 0)
                            {
                                MessageBox.Show("Staff already parked one vehicle during current vehile's parked period. Staff are not allowed to park multiple vehicle at a time", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                return;
                            }
                        }
                    }
                }
                POUT.CashAmount = 0;
                ExecuteSave(null);
                if (StaffBarcode != null)
                {
                    StaffBarcode.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                POUT.STAFF_BARCODE = null;
            }
        }



        private void SaveWithPrepaid(object obj)
        {
            if (obj == null)
            {
                MessageBox.Show("Invalid Card");
            }
            if (ValidateKKFC(GlobalClass.PrepaidInfo.GetValue("Url1").ToString(), GlobalClass.PrepaidInfo.GetValue("Url2").ToString(), GlobalClass.PrepaidInfo.GetValue("ClientId").ToString(), GlobalClass.PrepaidInfo.GetValue("ClientSecretKey").ToString(), obj.ToString(), POUT.PID.ToString(), TrnMode))
            {
                if (StaffBarcode != null)
                {
                    StaffBarcode.Close();
                    ExecuteSave(null);
                }
                else
                {
                    MessageBox.Show("Amount deducted sucessfully.");
                }
            }
        }

        bool ValidateKKFC(string Url1, string Url2, string ClientId, string ClientSecretKey, string CardNumber, string TransactionId, string TrnMode)
        {
            try
            {
                var PinRequest = (HttpWebRequest)WebRequest.Create(Url1);
                var ClinetInfo = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new { ClientId, ClientSecretKey }));
                PinRequest.Method = "POST";
                PinRequest.ContentType = "application/json";
                PinRequest.ContentLength = ClinetInfo.Length;

                using (var stream = (PinRequest.GetRequestStream()))
                {
                    stream.Write(ClinetInfo, 0, ClinetInfo.Length);
                }
                var response = (HttpWebResponse)PinRequest.GetResponse();
                var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                if (result.GetValue("status").ToString() != "ok")
                {
                    MessageBox.Show(result.GetValue("message").ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                var PinNumber = result.GetValue("result").ToString();

                var PaymentRequest = (HttpWebRequest)WebRequest.Create(Url2);
                var PaymentInfo = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new
                {
                    ClientId,
                    ClientSecretKey,
                    Amount = POUT.CashAmount,
                    CardNumber,
                    PinNumber,
                    TransactionId,
                    Description = POUT.BILLTO?.ToString(),
                    TransactionNumber = TransactionId,
                    TrnMode,
                    remarks = POUT.Remarks
                }));
                PaymentRequest.Method = "POST";
                PaymentRequest.ContentType = "application/json";
                PaymentRequest.ContentLength = PaymentInfo.Length;

                using (var stream = (PaymentRequest.GetRequestStream()))
                {
                    stream.Write(PaymentInfo, 0, PaymentInfo.Length);
                }
                var PaymentResponse = (HttpWebResponse)PaymentRequest.GetResponse();
                var ResponseMessage = new System.IO.StreamReader(PaymentResponse.GetResponseStream()).ReadToEnd();
                result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(ResponseMessage);
                if (result.GetValue("status").ToString() != "ok")
                {
                    MessageBox.Show(result.GetValue("message").ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                POUT.STAFF_BARCODE = PinNumber + ":" + result.GetValue("result").ToString();
                return true;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    var res = JsonConvert.DeserializeObject<dynamic>(Response);
                    MessageBox.Show(res.Message.ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                }
                else
                {
                    MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void OpenStaffBarcode(object obj)
        {
            if (obj.ToString() == "Staff")
            {
                StaffBarcode = new wStaffBarcode() { DataContext = this };
                StaffBarcode.ShowDialog();
            }
            else
            {
                DiscountScheme discountScheme = DiscountList.Where(x => x.DiscountPercent == 25).FirstOrDefault();
                this.SelectedDiscount = discountScheme;
                StaffBarcode = new wPrepaidCard() { DataContext = this };
                StaffBarcode.ShowDialog();
            }
        }

        private void ExecutePrint(object obj)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    string BillNo = InvoicePrefix + InvoiceNo;
                    string DuplicateCaption = GlobalClass.GetReprintCaption(BillNo);
                    PrintBill(BillNo, conn, (TaxInvoice) ? "INVOICE" : "ABBREVIATED TAX INVOCE", DuplicateCaption);
                    GlobalClass.SavePrintLog(BillNo, null, DuplicateCaption);
                    GlobalClass.SetUserActivityLog("Exit", "Re-Print", WorkDetail: string.Empty, VCRHNO: BillNo, Remarks: "Reprinted : " + DuplicateCaption);
                }
                ExecuteUndo(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave(object obj)
        {
            return _action == ButtonAction.Selected && !TaxInvoice;
        }
        private void CalculateAmount()
        {
            int IntervalMins, TotIntervalMins;
            List<RateDetails> RDetails;
            try
            {
                POUT.ChargedAmount = 0;
                POUT.CashAmount = 0;
                POUT.RoyaltyAmount = 0;
                IntervalMins = getMinutes(POUT.Interval);
                TotIntervalMins = IntervalMins;
                RDetails = new List<RateDetails>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteLoad(object obj)


        {
            try
            {
                decimal ChargedHours = 0;
                decimal ChargedAmount = 0;
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    if (_action == ButtonAction.Selected)
                    {
                        if (obj.ToString().StartsWith("#"))
                        {
                            ValidateVoucher(obj, conn);
                            return;
                        }
                        else if (obj.ToString().StartsWith(GlobalClass.MemberBarcodePrefix))
                        {
                            ValidateMember(obj, conn);
                            return;
                        }
                        else
                        {
                            POUT.SaveLog(conn);
                            Vouchers.Clear();
                            mDiscount = null;
                        }
                    }

                    var PINS = conn.Query<ParkingIn>(string.Format(@"SELECT PID, VehicleType, InDate, InMiti, InTime, PlateNo, Barcode, UID FROM ParkingInDetails 
WHERE((BARCODE <> '' AND  BARCODE = '{0}') OR(ISNULL(PLATENO, '') <> '' AND ISNULL(PlateNo, '') = '{0}'))
AND FYID = {1} ORDER BY PID DESC", obj, GlobalClass.FYID));
                    if (PINS.Count() <= 0)
                    {
                        MessageBox.Show("Invalid barcode readings.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        PIN.Barcode = string.Empty;
                        return;
                    }
                    PIN = PINS.First();
                    PIN.VType = conn.Query<VehicleType>(string.Format("SELECT VTypeId, Description FROM VehicleType WHERE VTypeId = {0}", PIN.VehicleType)).First();
                    var POUTS = conn.Query<ParkingOut>(string.Format("SELECT * FROM ParkingOutDetails WHERE PID = {0} AND FYID = {1}", PIN.PID, GlobalClass.FYID));
                    if (POUTS.Count() > 0)
                    {
                        //POUT = POUTS.First();
                        MessageBox.Show("Entity already exited", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        PIN.Barcode = string.Empty;
                        return;
                    }
                    POUT.Rate_ID = (int)conn.ExecuteScalar("SELECT RATE_ID FROM RATEMASTER WHERE IsDefault = 1");

                    DateTime ServerTime = nepDate.GetServerTime();
                    POUT.OutDate = ServerTime.Date;
                    POUT.OutTime = ServerTime.ToString("hh:mm:ss tt");
                    POUT.OutMiti = nepDate.CBSDate(POUT.OutDate);
                    POUT.Interval = GetInterval(PIN.InDate, POUT.OutDate, PIN.InTime, POUT.OutTime);
                    POUT.PID = PIN.PID;

                    CalculateParkingCharge(conn, PIN.InDate.Add(DateTime.Parse(PIN.InTime).TimeOfDay), POUT.OutDate.Add(DateTime.Parse(POUT.OutTime).TimeOfDay), POUT.Rate_ID, PIN.VehicleType, ref ChargedAmount, ref ChargedHours);
                    POUT.ChargedHours = ChargedHours;
                    POUT.ChargedAmount = ChargedAmount;
                    POUT.CashAmount = POUT.ChargedAmount;
                    SetAction(ButtonAction.Selected);
                    if (POUT.CashAmount > GlobalClass.AbbTaxInvoiceLimit)
                    {
                        TaxInvoice = true;
                        CanChangeInvoiceType = false;
                    }
                    else
                    {
                        CanChangeInvoiceType = true;
                    }
                    PIN.Barcode = string.Empty;
                    FocusedElement = (short)Focusable.Finish;
                    SaveWithStaffEnabled = true;
                    IsHoliday = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Holiday WHERE HolidayDate = @HolidayDate", new { HolidayDate = POUT.OutDate }) > 0;
                    PoleDisplay.WriteToDisplay(POUT.ChargedAmount, PoleDisplayType.AMOUNT);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateMember(object obj, SqlConnection conn)
        {
            if (mDiscount != null)
            {
                return;
            }

            TimeSpan InTime = DateTime.Parse(PIN.InTime).TimeOfDay;
            TimeSpan OutTime = DateTime.Parse(POUT.OutTime).TimeOfDay;
            decimal DiscountAmount = 0;
            decimal DiscountHour = 0;
            int Interval;

            Member m = conn.Query<Member>("SELECT MemberId, MemberName, SchemeId, ExpiryDate, ActivationDate, Barcode, Address FROM Members WHERE Barcode = @MemberId ", new { MemberId = obj.ToString() }).FirstOrDefault();
            if (m == null)
            {
                MessageBox.Show("The member does not exists.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (m.ActivationDate > POUT.OutDate || m.ExpiryDate < POUT.OutDate)
            {
                MessageBox.Show("The membership is expired or not yet activated.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            MembershipScheme scheme = conn.Query<MembershipScheme>("SELECT * FROM MembershipScheme WHERE SchemeId = @SchemeId", m).FirstOrDefault();
            if (scheme == null)
            {
                MessageBox.Show("Membership scheme does not exists.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (!scheme.ValidOnWeekends && POUT.OutDate.DayOfWeek == DayOfWeek.Saturday)
            {
                MessageBox.Show("The Membership is not valid on Weekends", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (!scheme.ValidOnHolidays && conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Holiday WHERE HolidayDate = @HolidayDate", new { HolidayDate = POUT.OutDate }) > 0)
            {
                MessageBox.Show("The Membership is not valid on Public Holidays", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            List<dynamic> TimeSpentInEachSession = GetTimeSpentInEachSession(InTime, OutTime, scheme);
            //else if (!scheme.ValidHoursList.Any(x => x.Start < outTime && x.End > outTime))
            //{
            //    MessageBox.Show("Membership is not valid for current Shift.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            //    return;
            //}
            Interval = conn.ExecuteScalar<int>("SELECT ISNULL(SUM(MDD.Interval - MDD.SkipInterval),0) Interval FROM MemberDiscountDetail MDD JOIN ParkingOutDetails POD ON MDD.PID = POD.PID WHERE MemberId = @MemberId AND POD.OutDate = @OutDate", new { MemberId = m.MemberId, OutDate = POUT.OutDate });
            if (Interval >= scheme.Limit && !TimeSpentInEachSession.Any(x => x.SkipValidityPeriod && x.TimeSpent > 0))
            {
                MessageBox.Show("Free Entrance for the Member has exceeded for day.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }


            SaveWithStaffEnabled = false;
            mDiscount = new MemberDiscount
            {
                MemberId = m.MemberId,
                SchemeId = m.SchemeId,
                FYID = POUT.FYID,
                PID = PIN.PID,
            };
            POUT.BILLTO = m.MemberName;
            POUT.BILLTOADD = m.Address;

            if (TimeSpentInEachSession.Any(x => x.SkipValidityPeriod))
            {
                mDiscount.SkipInterval = TimeSpentInEachSession.Where(x => x.SkipValidityPeriod).Sum(x => x.TimeSpent);
            }

            int DiscountInterval = scheme.Limit - Interval + mDiscount.SkipInterval;
            foreach (dynamic session in TimeSpentInEachSession.Where(x => x.TimeSpent > 0))
            {
                if (session.IgnoreLimit)
                {
                    CalculateParkingCharge(conn, session.Start, session.End, POUT.Rate_ID, PIN.VehicleType, ref DiscountAmount, ref DiscountHour);
                    DiscountInterval -= session.TimeSpent;
                }
                else
                {
                    CalculateParkingCharge(conn, session.Start, (DiscountInterval < session.TimeSpent) ? session.Start.AddMinutes(DiscountInterval) : session.End, POUT.Rate_ID, PIN.VehicleType, ref DiscountAmount, ref DiscountHour);
                    DiscountInterval -= (DiscountInterval < session.TimeSpent) ? DiscountInterval : session.TimeSpent;
                }
                mDiscount.Interval += DiscountHour * 60;
                mDiscount.DiscountAmount += DiscountAmount * scheme.Discount / 100;
                DiscountHour = 0;
                DiscountAmount = 0;
            }
            POUT.CashAmount = POUT.ChargedAmount - mDiscount.DiscountAmount;
            PIN.Barcode = string.Empty;
            if (POUT.CashAmount == 0)
            {
                ExecuteSave(null);
            }

            PoleDisplay.WriteToDisplay(POUT.CashAmount, PoleDisplayType.AMOUNT);
        }

        List<dynamic> GetTimeSpentInEachSession(TimeSpan InTime, TimeSpan OutTime, MembershipScheme scheme)
        {
            //TimeSpan Start = InTime;
            DateTime Start = PIN.InDate;
            DateTime End = PIN.InDate;
            TimeSpan Minute = new TimeSpan(0, 1, 0);
            List<dynamic> TimeSpentInEachSession = new List<dynamic>();
            //while (Start < OutTime)
            //{
            //    TimeSpan TimeSpent;
            //    var Session = scheme.ValidHoursList.FirstOrDefault(x => x.Start <= Start && x.End >= Start);

            //    if (OutTime > Session.End.Add(Minute))
            //        TimeSpent = Session.End.Subtract(Start).Add(Minute);
            //    else
            //        TimeSpent = OutTime.Subtract(Start);

            //    TimeSpentInEachSession.Add(new { Session.SkipValidityPeriod, TimeSpent });
            //    Start = Start.Add(TimeSpent);
            //}
            foreach (var session in scheme.ValidHoursList)
            {
                TimeSpan TimeSpent = new TimeSpan(0, 0, 0);
                if (InTime >= session.Start && InTime <= session.End)
                {
                    if (OutTime <= session.End.Add(Minute))
                    {
                        TimeSpent = OutTime.Subtract(InTime);
                        End = PIN.InDate.Add(OutTime);
                    }
                    else
                    {
                        TimeSpent = session.End.Add(Minute).Subtract(InTime);
                        End = PIN.InDate.Add(session.End.Add(Minute));
                    }
                    Start = PIN.InDate.Add(InTime);

                }
                else if (InTime < session.Start)
                {
                    if (OutTime <= session.End.Add(Minute))
                    {
                        TimeSpent = OutTime.Subtract(session.Start);
                        End = PIN.InDate.Add(OutTime);
                    }
                    else
                    {
                        TimeSpent = session.End.Add(Minute).Subtract(session.Start);
                        End = PIN.InDate.Add(session.End.Add(Minute));
                    }
                    Start = PIN.InDate.Add(session.Start);
                }
                TimeSpentInEachSession.Add(new { session.SkipValidityPeriod, session.IgnoreLimit, TimeSpent = Convert.ToInt32(TimeSpent.TotalMinutes), Start, End });
            }
            return TimeSpentInEachSession;
        }

        private void ValidateVoucher(object obj, SqlConnection conn)
        {
            if (Vouchers.Any(x => x.Barcode == obj.ToString()))
            {
                return;
            }

            Voucher v = conn.Query<Voucher>("SELECT VoucherNo, Barcode, VoucherId, Value,ValuePercent, ExpDate, ValidStart, ValidEnd, ScannedTime FROM ParkingVouchers WHERE Barcode = @Barcode", new { Barcode = obj.ToString() }).FirstOrDefault();
            if (v == null)
            {
                MessageBox.Show("Invalid Voucher", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (v.ScannedTime == null)
            {
                MessageBox.Show("Voucher already redeemed.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (!VoucherTypes.Any(x => x.VoucherId == v.VoucherId && (x.VehicleType == 0 || x.VehicleType == PIN.VehicleType)))
            {
                MessageBox.Show("The Voucher is not valid for current Entrance Type.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (v.ExpDate < CurDate)
            {
                MessageBox.Show("Voucher has expired.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                TimeSpan outTime = Convert.ToDateTime(POUT.OutTime).TimeOfDay;
                if (v.ValidStart < v.ValidEnd)
                {
                    if (outTime < v.ValidStart || outTime > v.ValidEnd)
                    {
                        MessageBox.Show("Voucher is not valid for current Shift.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                else
                {
                    if (outTime < v.ValidStart && outTime > v.ValidEnd)
                    {
                        MessageBox.Show("Voucher is not valid for current Shift.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
            }
            SaveWithStaffEnabled = false;

            if (v.ValuePercent > 0)
            {
                v.Value = POUT.CashAmount * v.ValuePercent / 100;
                v.Value = (POUT.CashAmount > v.Value) ? v.Value : POUT.CashAmount;
            }
            else
            {
                v.Value = (POUT.CashAmount > v.Value) ? v.Value : POUT.CashAmount;
            }
            POUT.CashAmount = POUT.CashAmount - v.Value;
            PIN.Barcode = string.Empty;
            Vouchers.Add(v);
            if (POUT.CashAmount == 0)
            {
                ExecuteSave(null);
            }

            PoleDisplay.WriteToDisplay(POUT.CashAmount, PoleDisplayType.AMOUNT);
        }

        void CalculateParkingCharge(SqlConnection conn, DateTime InTime, DateTime OutTime, int RateId, int VehicleID, ref decimal ChargedAmount, ref decimal ChargedHours)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                cmd.CommandText = "sp_Calculate_PCharge";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InTime", InTime);
                cmd.Parameters.AddWithValue("@OutTime", OutTime);
                cmd.Parameters.AddWithValue("@RateId", RateId);
                cmd.Parameters.AddWithValue("@VehicleId", VehicleID);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        ChargedAmount = GParse.ToDecimal(dr[0]);
                        ChargedHours = (decimal)dr[1];
                    }
                }
            }
        }


        private void ExecuteSave(object obj)
        {
            string strSQL;
            decimal Taxable, VAT, Amount, Discount = 0, NonTaxable, Rate, Quantity;
            string BillNo = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        POUT.SESSION_ID = GlobalClass.Session;
                        POUT.Save(tran);
                        if (POUT.CashAmount > 0)
                        {
                            BillNo = InvoicePrefix + GlobalClass.GetInvoiceNo(InvoicePrefix, tran);
                            Quantity = POUT.ChargedHours;
                            if (Vouchers.Count > 0)
                            {
                                Discount = Vouchers.Sum(x => x.Value);
                            }
                            else if (mDiscount != null)
                            {
                                Discount = mDiscount.DiscountAmount;
                            }
                            else if (POUT.CashAmount < POUT.ChargedAmount)
                            {
                                Discount = POUT.ChargedAmount - POUT.CashAmount;
                            }
                            Amount = POUT.ChargedAmount / (1 + (GlobalClass.VAT / 100));
                            Discount = Discount / (1 + (GlobalClass.VAT / 100));
                            Rate = Amount / Quantity;
                            NonTaxable = 0;
                            Taxable = Amount - (NonTaxable + Discount);
                            VAT = Taxable * GlobalClass.VAT / 100;

                            TParkingSales PSales = new TParkingSales
                            {
                                BillNo = BillNo,
                                TDate = POUT.OutDate,
                                TMiti = POUT.OutMiti,
                                TTime = POUT.OutTime,
                                BillTo = POUT.BILLTO,
                                BILLTOADD = POUT.BILLTOADD,
                                BILLTOPAN = POUT.BILLTOPAN,
                                Amount = Amount,
                                Discount = Discount,
                                NonTaxable = NonTaxable,
                                Taxable = Taxable,
                                VAT = VAT,
                                GrossAmount = POUT.CashAmount,
                                PID = POUT.PID,
                                UID = POUT.UID,
                                SESSION_ID = POUT.SESSION_ID,
                                FYID = GlobalClass.FYID,
                                TaxInvoice = TaxInvoice
                            };
                            PSales.Save(tran);
                            TParkingSalesDetails PSalesDetails = new TParkingSalesDetails
                            {
                                BillNo = BillNo,
                                PType = 'P',
                                Description = "Parking Charge",
                                FYID = GlobalClass.FYID,
                                Quantity = Quantity,
                                Rate = Rate,
                                Amount = Amount,
                                Discount = Discount,
                                NonTaxable = NonTaxable,
                                Taxable = Taxable,
                                VAT = VAT,
                                NetAmount = POUT.CashAmount,
                            };
                            PSalesDetails.Save(tran);

                            conn.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = InvoicePrefix, FYID = GlobalClass.FYID }, transaction: tran);
                            GlobalClass.SetUserActivityLog(tran, "Exit", "New", VCRHNO: BillNo, WorkDetail: "Bill No : " + BillNo);
                            SyncFunctions.LogSyncStatus(tran, BillNo, GlobalClass.FYNAME);
                        }
                        if (Vouchers.Count > 0)
                        {
                            strSQL = "INSERT INTO VoucherDiscountDetail (BillNo, FYID, VoucherNo, DiscountAmount, UID) VALUES (@BillNo, @FYID, @VoucherNo, @DiscountAmount, @UID)";
                            foreach (Voucher v in Vouchers)
                            {
                                conn.Execute(strSQL, new
                                {
                                    BillNo = string.IsNullOrEmpty(BillNo) ? "CS1" : BillNo,
                                    FYID = GlobalClass.FYID,
                                    VoucherNo = v.VoucherNo,
                                    DiscountAmount = v.Value,
                                    UID = POUT.UID
                                }, transaction: tran);
                                conn.Execute("UPDATE ParkingVouchers SET ScannedTime = GETDATE() WHERE VoucherNo = @VoucherNo", v, tran);
                            }
                        }
                        else if (mDiscount != null)
                        {
                            mDiscount.BillNo = string.IsNullOrEmpty(BillNo) ? "MS1" : BillNo;
                            mDiscount.Save(tran);
                        }
                        tran.Commit();
                        if (!string.IsNullOrEmpty(SyncFunctions.username) && POUT.CashAmount > 0)
                        {
                            SyncFunctions.SyncSalesData(SyncFunctions.getBillObject(BillNo), 1);
                        }
                    }
                    if (!string.IsNullOrEmpty(BillNo))
                    {
                        RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, ((char)27).ToString() + ((char)112).ToString() + ((char)0).ToString() + ((char)64).ToString() + ((char)240).ToString(), "Receipt");   //Open Cash Drawer
                        PrintBill(BillNo.ToString(), conn, (TaxInvoice) ? "TAX INVOICE" : "ABBREVIATED TAX INVOCE");
                        if (TaxInvoice)
                        {
                            PrintBill(BillNo.ToString(), conn, "INVOICE");
                        }
                    }
                    SetAction(ButtonAction.Init);
                    ExecuteUndo(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteUndo(object obj)
        {
            if (_action == ButtonAction.Selected)
            {
                if (POUT.ChargedHours > 0)
                {
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        POUT.SaveLog(conn);
                    }
                }
            }
            FocusedElement = (short)Focusable.Barcode;
            TaxInvoice = false;
            CanChangeInvoiceType = true;
            PIN = new ParkingIn();
            POUT = new ParkingOut();
            POUT.PropertyChanged += POUT_PropertyChanged;
            SetAction(ButtonAction.Init);
            Vouchers.Clear();
            mDiscount = null;
            SelectedDiscount = null;
            OnPropertyChanged("IsEntryMode");
            PoleDisplay.WriteToDisplay(0);
        }

        string GetInterval(DateTime In, DateTime Out, string InTime, string OutTime)
        {
            var InDate = In.Add(DateTime.Parse(InTime).TimeOfDay);
            var OutDate = Out.Add(DateTime.Parse(OutTime).TimeOfDay);
            var interval = OutDate - InDate;
            return (interval.Days * 24 + interval.Hours).ToString() + " Hrs " + (interval.Minutes).ToString() + " Mins";
        }

        int getMinutes(string duration)
        {
            string[] hrsMins;
            int hrs = 0, min = 0, TotMins;
            if (string.IsNullOrEmpty(duration) || duration == "N/A")
            {
                return 0;
            }

            try
            {
                if (duration.Contains(" "))
                {
                    hrsMins = duration.Split(' ');
                    if (hrsMins.Length == 2)
                    {
                        hrs = GParse.ToInteger(hrsMins[0].Replace("Hrs", string.Empty));
                        min = GParse.ToInteger(hrsMins[1].Replace("Min", string.Empty));
                    }
                    else if (hrsMins.Length == 4)
                    {
                        hrs = GParse.ToInteger(hrsMins[0]);
                        min = GParse.ToInteger(hrsMins[2]);
                    }
                }
                else if (duration.Contains("Hrs"))
                {
                    hrs = GParse.ToInteger(duration.Replace("Hrs", string.Empty));
                    min = 0;
                }
                else
                {
                    hrs = 0;
                    min = GParse.ToInteger(duration.Replace("Min", string.Empty));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            TotMins = hrs * 60 + min;
            return TotMins;

        }

        void LoadRateSchemes()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                VoucherTypes = conn.Query<VoucherType>("SELECT VoucherId, VehicleType FROM VoucherTypes").ToList();
                RSchemes = new ObservableCollection<RateMaster>(conn.Query<RateMaster>("SELECT Rate_ID, RateDescription, IsDefault, [UID] FROM RATEMASTER"));
                DiscountList = conn.Query<DiscountScheme>("SELECT * FROM DiscountScheme WHERE ExpiryDate >= GETDATE()").ToList();
                DiscountList.Insert(0, new DiscountScheme
                {
                    SchemeId = 0,
                    SchemeName = "No Discount",
                    DiscountPercent = 0,
                    MaxHrs = int.MaxValue,
                    ExpiryDate = new DateTime(2100, 1, 1),
                    ValidHours = "[{'Start':'00:00:00','End':'23:59:59'}]",
                    ValidOnHolidays = true,
                    ValidOnWeekends = true
                });
            }
            GlobalClass.DefaultRate = RSchemes.First(x => x.IsDefault);
        }


        public static void PrintBill(string BillNo, SqlConnection conn, string InvoiceName, string DuplicateCaption = "")
        {
            DataRow dr;
            //// RawPrinterHelper printer = new RawPrinterHelper();
            using (DataAccess da = new DataAccess())
            {
                dr = da.getData(string.Format(@"SELECT PS.*,VT.Description VType,ISNULL(PIN.PlateNo,'') PlateNo,PIN.InTime,PIN.InMiti,POUT.OutTime,POUT.OutMiti,U.UserName, POUT.Interval, POUT.ChargedHours FROM ParkingSales PS 
                                    INNER JOIN Users U ON U.UID=PS.UID
                                    LEFT JOIN ParkingOutDetails POUT  ON PS.PID = POUT.PID AND PS.FYID = POUT.FYID
                                    LEFT JOIN (ParkingInDetails PIN   
                                    LEFT JOIN VehicleType VT ON VT.VTypeID=PIN.VehicleType) ON PS.PID = PIN.PID AND PS.FYID = PIN.FYID
                                    WHERE BillNo = '{0}' AND PS.FYID = {1}", BillNo, GlobalClass.FYID), conn).Rows[0];

            }
            string InWords = GlobalClass.GetNumToWords(conn, Convert.ToDecimal(dr["GrossAmount"]));
            string strPrint = string.Empty;
            int PrintLen = 40;
            string Description = dr["Description"].ToString();
            string Particulars = "Particulars";
            string PAN = "PAN : " + GlobalClass.CompanyPan;
            Description = (Description.Length > PrintLen - 17) ? Description.Substring(0, PrintLen - 17) : Description.PadRight(PrintLen - 17, ' ');
            Particulars = (Particulars.Length > PrintLen - 17) ? Particulars.Substring(0, PrintLen - 17) : Particulars.PadLeft((PrintLen + Particulars.Length - 17) / 2, ' ').PadRight(PrintLen - 17, ' ');

            strPrint += (GlobalClass.CompanyName.Length > PrintLen) ? GlobalClass.CompanyName.Substring(0, PrintLen) : GlobalClass.CompanyName.PadLeft((PrintLen + GlobalClass.CompanyName.Length) / 2, ' ') + Environment.NewLine;
            strPrint += (GlobalClass.CompanyAddress.Length > PrintLen) ? GlobalClass.CompanyAddress.Substring(0, PrintLen) : GlobalClass.CompanyAddress.PadLeft((PrintLen + GlobalClass.CompanyAddress.Length) / 2, ' ') + Environment.NewLine;
            strPrint += PAN.PadLeft((PrintLen + PAN.Length) / 2, ' ') + Environment.NewLine;
            strPrint += InvoiceName.PadLeft((PrintLen + InvoiceName.Length) / 2, ' ') + Environment.NewLine;
            if (!string.IsNullOrEmpty(DuplicateCaption))
            {
                strPrint += DuplicateCaption.PadLeft((PrintLen + DuplicateCaption.Length) / 2, ' ') + Environment.NewLine;
            }

            strPrint += string.Format("Bill No : {0}    Date : {1}", BillNo.PadRight(7, ' '), dr["TMiti"]) + Environment.NewLine;
            strPrint += string.Format("Vehicle Type : {0} {1}", dr["VType"], string.IsNullOrEmpty(dr["PlateNo"].ToString()) ? string.Empty : "(" + dr["PlateNo"] + ")") + Environment.NewLine;
            strPrint += string.Format("Name    : {0}", dr["BillTo"]) + Environment.NewLine;
            strPrint += string.Format("Address : {0}", dr["BillToAdd"]) + Environment.NewLine;
            strPrint += string.Format("PAN     : {0}", dr["BillToPan"]) + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Sn.|{0}|  Amount  |", Particulars) + Environment.NewLine;
            strPrint += string.Format("1.  {0}  {1}", Description, GParse.ToDecimal(((bool)dr["TaxInvoice"]) ? dr["Amount"] : dr["GrossAmount"]).ToString("#0.00").PadLeft(8, ' ')) + Environment.NewLine;
            strPrint += string.Format("    IN  : {0} {1}", dr["InTime"], dr["InMiti"]) + Environment.NewLine;
            strPrint += string.Format("    OUT : {0} {1}", dr["OutTime"], dr["OutMiti"]) + Environment.NewLine;
            strPrint += string.Format("    Interval : {0} ", dr["Interval"]) + Environment.NewLine;
            strPrint += string.Format("    Charged Hours : {0} ", dr["ChargedHours"]) + Environment.NewLine;

            strPrint += Environment.NewLine;
            strPrint += "------------------------".PadLeft(PrintLen, ' ') + Environment.NewLine;
            if ((bool)dr["TaxInvoice"])
            {
                strPrint += ("Gross Amount : " + GParse.ToDecimal(dr["Amount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                if (GParse.ToDecimal(dr["Discount"]) > 0)
                {
                    strPrint += ("Discount : " + GParse.ToDecimal(dr["Discount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                }

                strPrint += ("Taxable : " + GParse.ToDecimal(dr["Taxable"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                strPrint += ("Non Taxable : " + GParse.ToDecimal(dr["NonTaxable"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
                strPrint += ("VAT 13% : " + GParse.ToDecimal(dr["VAT"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ') + Environment.NewLine;
            }
            strPrint += ("Net Amount : " + GParse.ToDecimal(dr["GrossAmount"]).ToString("#0.00").PadLeft(8, ' ')).PadLeft(PrintLen, ' ');
            strPrint += Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += InWords + Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += string.Format("Cashier : {0} ({1})", dr["UserName"], dr["TTime"]) + Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += Environment.NewLine;
            strPrint += "".PadRight(PrintLen, '-') + Environment.NewLine;
            strPrint += ((char)29).ToString() + ((char)86).ToString() + ((char)1).ToString();

            if (GlobalClass.NoRawPrinter)
            {
                new StringPrint(strPrint).Print();
            }
            else
            {
                RawPrinterHelper.SendStringToPrinter(GlobalClass.PrinterName, strPrint, "Receipt");
            }
        }


        void POUT_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Rate_ID")
            {
                CalculateAmount();
                POUT.ChargedAmount = POUT.CashAmount;
            }
        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (CurTime != DateTime.Now.ToString("hh:mm tt"))
            {
                if (DateTime.Now.Second > 5)
                {
                    timer.Interval = new TimeSpan(0, 0, 1);
                }
                else
                {
                    timer.Interval = new TimeSpan(0, 1, 0);
                }
            }
            CurTime = DateTime.Now.ToString("hh:mm tt");
            CurDate = DateTime.Today;
        }
    }

}
