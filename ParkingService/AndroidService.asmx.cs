using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using Dapper;
namespace LabimParking
{
    /// <summary>
    /// Summary description for AndroidService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class AndroidService : System.Web.Services.WebService
    {

        [WebMethod]
        public string Login(string UserName, string Password, string MAC)
        {
            int Session_Id;
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var user = conn.Query(string.Format("SELECT UID, UserName, [Password], FullName, UserCat, [STATUS],  MOBILE_ACCESS, SALT, PA_ASSIGN, PA_STATUS, PA_LOG  FROM USERS WHERE UserName = '{0}'", UserName)).First();
                    if (user == null)
                    {
                        return "Invalid username or password.";

                    }
                    string salt = user.SALT;
                    if (user.Password != GlobalClass.GetEncryptedPWD(Password, ref salt))
                    {
                        return "Invalid username or password.";

                    }
                    if (user.MOBILE_ACCESS != 1)
                    {
                        return "You do not have privilage to access this application";
                    }
                    if (user.STATUS != 0)
                    {
                        return "You no longer have privilage to access this application";
                    }

                    Session_Id = GlobalClass.StartSession(MAC, user.UID);
                    if (Session_Id < 1)
                    {
                        return "Session could not be started. Restart the application and try again.";
                    }

                    string str = "{{\"UID\" : \"{0}\", \"UserName\" : \"{1}\", \"FullName\" : \"{2}\", \"UserCat\" : \"{3}\", \"PA_ASSIGN\" : \"{4}\", \"PA_STATUS\" : \"{5}\", \"PA_LOG\" : \"{6}\", \"Session\" : \"{7}\", \"Shift\" = \"{8}\"}}";
                    return string.Format(str, user.UID, UserName, user.FullName, user.UserCat, user.PA_ASSIGN, user.PA_STATUS, user.PA_LOG, Session_Id, "0");
                }

            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string ChangePassword(string UserName, string OldPassword, string NewPassword)
        {   
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var user = conn.Query(string.Format("SELECT UID, UserName, [Password], FullName, UserCat, [STATUS],  MOBILE_ACCESS, SALT, PA_ASSIGN, PA_STATUS, PA_LOG  FROM USERS WHERE UserName = '{0}'", UserName)).First();
                    if (user == null)
                    {
                        return "Invalid username or password.";

                    }
                    string salt = user.SALT;
                    if (user.Password != GlobalClass.GetEncryptedPWD(OldPassword, ref salt))
                    {
                        return "Invalid username or password.";
                    }


                    salt = string.Empty;
                    conn.Execute(string.Format("UPDATE Users SET [Password]='{0}', SALT = '{1}' WHERE UID = {2}", GlobalClass.GetEncryptedPWD(NewPassword, ref  salt), salt, (int)user.UID));
                    return "Success";
                }

            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string GetUsers()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var users = Conn.Query("SELECT UID, UserName, FullName  FROM USERS WHERE STATUS = 0 AND MOBILE_ACCESS = 1 AND PA_LOG = 1");
                    return Newtonsoft.Json.JsonConvert.SerializeObject(users);
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string GetShifts()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var users = Conn.Query("SELECT SHIFT_ID, SHIFT_NAME, SHIFT_START, SHIFT_END FROM tblShift WHERE SHIFT_STATUS = 1");
                    return Newtonsoft.Json.JsonConvert.SerializeObject(users);
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string GetParkingAreas()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var users = Conn.Query("SELECT PA_ID, PA_NAME, [Description], VehicleType, Capacity FROM ParkingArea");
                    return Newtonsoft.Json.JsonConvert.SerializeObject(users);
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string SetAssignment(string UID, string AssignedBy, string PA_ID, string SHIFT_ID)
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    if (Conn.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM tblPAshiftAssignment WHERE SHIFT_ID = {0} AND PA_ID = {1} AND UID = {2} AND ShiftDate = CONVERT(VARCHAR, GETDATE(),101)", SHIFT_ID, PA_ID, UID)) > 0)
                        return "User already assigned to selected parking area for selected shift";
                    if (Conn.ExecuteScalar<int>(string.Format("SELECT COUNT(*) FROM tblPAshiftAssignment WHERE UID = {0} AND ShiftDate = CONVERT(VARCHAR, GETDATE(),101)", UID)) >= 5)
                        return "A User cannot be assigned to more than 5 shift per day";
                    if (Conn.Execute(string.Format("INSERT INTO tblPAShiftAssignment(SHIFT_ID, PA_ID, [UID], SHIFTDate,AssignedBy) VALUES ({0},{1},{2},CONVERT(VARCHAR,GETDATE(),101),{3})", SHIFT_ID, PA_ID, UID, AssignedBy)) == 1)
                        return "Success";
                    else
                        return "Failure";
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }

        [WebMethod]
        public string SetLog(string UID, string PA_ID, string IsOut, string Session_ID)
        {
            try
            {
                int InFlag = (IsOut == "1") ? 0 : 1;
                int OutFlag = (IsOut == "1") ? 1 : 0;
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    dynamic PAStatus = Conn.Query<dynamic>(string.Format(@"SELECT PA.Capacity, SUM(InFlag) - SUM(OutFlag) Occupency 
                                        FROM ParkingAreaInOutlog L JOIN ParkingArea PA ON L.PA_ID = PA.PA_ID 
                                        JOIN VehicleType VT ON VT.VTYPEID = PA.VehicleType
                                        WHERE PA.PA_ID = {0}
                                        GROUP BY PA.PA_ID, PA.Capacity, VT.[DESCRIPTION], VT.VTypeID, PA.PA_NAME", PA_ID)).First();
                    if (OutFlag == 1 && PAStatus.Occupency <= 0)
                    {
                        return "No more vehicle in Parking Area";
                    }
                    else if (InFlag == 1 && PAStatus.Occupency >= PAStatus.Capacity)
                    {
                        return "Parking area is full.";
                    }
                    if (Conn.Execute(string.Format("INSERT INTO PARKINGAREAINOUTLOG( PA_ID, [UID],TrnTime, InFlag, OutFlag, SESSION_ID) VALUES ({0}, {1}, GETDATE(), {2}, {3},{4})", PA_ID, UID, InFlag, OutFlag, Session_ID)) == 1)
                        return "Success";
                    else
                        return "Failure";
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }


        [WebMethod]
        public string GetAssignedParkingAreas(int UID)
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    short Shift_Id = Conn.ExecuteScalar<short>("SELECT ISNULL(S.SHIFT_ID,0) FROM tblSHIFT S JOIN tblPAShiftAssignment PASA ON S.SHIFT_ID = PASA.SHIFT_ID WHERE PASA.Shiftdate = CONVERT(VARCHAR, GETDATE(), 101) AND  CONVERT(VARCHAR, GETDATE(), 114) BETWEEN S.SHIFT_START AND S.SHIFT_END AND PASA.[UID] = " + UID);
                    if (Shift_Id <= 0)
                    {
                        return "[]";
                    }
                    var users = Conn.Query(string.Format("SELECT PA.PA_ID, PA_NAME, ISNULL([Description],'') [Description], VehicleType, Capacity FROM ParkingArea PA JOIN tblPAShiftAssignment PASA ON PA.PA_ID = PASA.PA_ID WHERE PASA.UID = {0} AND PASA.SHIFT_ID = {1} AND ShiftDate = CONVERT(VARCHAR, GETDATE(), 101)", UID, Shift_Id));
                    return Newtonsoft.Json.JsonConvert.SerializeObject(users);
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }


        [WebMethod]
        public string GetPAStatus()
        {
            try
            {
                using (SqlConnection Conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var users = Conn.Query(@"SELECT PA.PA_ID, PA.PA_NAME,  VT.VTypeID , VT.[DESCRIPTION] VehicleType, PA.Capacity, SUM(InFlag) - SUM(OutFlag) Occupency , PA.Capacity - (SUM(InFlag) - SUM(OutFlag)) Available
                                        FROM ParkingAreaInOutlog L JOIN ParkingArea PA ON L.PA_ID = PA.PA_ID 
                                        JOIN VehicleType VT ON VT.VTYPEID = PA.VehicleType
                                        GROUP BY PA.PA_ID, PA.Capacity, VT.[DESCRIPTION], VT.VTypeID, PA.PA_NAME");
                    return Newtonsoft.Json.JsonConvert.SerializeObject(users);
                }
            }
            catch (Exception ex)
            {
                return GlobalClass.GetRootException(ex).Message;
            }
        }


    }
}
