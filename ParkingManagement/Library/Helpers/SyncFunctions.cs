using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ParkingManagement.Models;
namespace ParkingManagement.Library.Helpers
{
    static class SyncFunctions
    {
        const string Url = "http://202.166.207.75:9050/api";
        public static string username { get; private set; }
        static string password;
        public static bool CbmsTest;

        static SyncFunctions()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                username = conn.ExecuteScalar<string>("SELECT ISNULL(IrdApiUser,'') FROM tblSetting");
                password = conn.ExecuteScalar<string>("SELECT ISNULL(IrdApiPassword,'') FROM tblSetting");
            }
        }

        public static BillViewModel getBillObject(string BillNo)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                BillViewModel bvm = conn.Query<BillViewModel>("SELECT BILLTOPAN buyer_pan, F.FYNAME fiscal_year, BILLTO buyer_name, BillNo  invoice_number, RIGHT(TMiti, 4) + '.' + SUBSTRING(TMiti,4,2) + '.' + LEFT(TMiti, 2) invoice_date, GrossAmount total_sales, TAXABLE taxable_sales_vat, NonTaxable tax_exempted_sales, VAT vat, GETDATE() dateTimeClient FROM ParkingSales PS JOIN tblFiscalYear F ON PS.FYID = F.FYID WHERE BillNo = @BillNo AND PS.FYID = @FYID", new { BillNo, GlobalClass.FYID }).FirstOrDefault();
                bvm.isrealtime = true;
                bvm.seller_pan = CbmsTest ? "999999999" : GlobalClass.CompanyPan;
                return bvm;
            }
        }

        public static BillReturnViewModel getBillReturnObject(string BillNo)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                BillReturnViewModel bvm = conn.Query<BillReturnViewModel>("SELECT BILLTOPAN buyer_pan, F.FYNAME fiscal_year, BILLTO buyer_name, BillNo  credit_note_number, RefBillNo ref_invoice_number, Remarks reason_for_return, RIGHT(TMiti, 4) + '.' + SUBSTRING(TMiti,4,2) + '.' + LEFT(TMiti, 2) credit_note_date, GrossAmount total_sales, TAXABLE taxable_sales_vat, NonTaxable tax_exempted_sales, VAT vat, GETDATE() dateTimeClient FROM ParkingSales PS JOIN tblFiscalYear F ON PS.FYID = F.FYID WHERE BillNo = @BillNo AND PS.FYID = @FYID", new { BillNo, GlobalClass.FYID }).FirstOrDefault();
                bvm.seller_pan = CbmsTest ? "999999999" : GlobalClass.CompanyPan;
                bvm.isrealtime = true;
                return bvm;
            }
        }
        public static async Task<bool> SyncSalesData(BillViewModel SalesModel, byte isRealTime = 0)
        {
            try
            {
                SalesModel.username = username;
                SalesModel.password = password;
                var request = (HttpWebRequest)WebRequest.Create(Url + "/bill");
                var data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(SalesModel));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (var stream = (await request.GetRequestStreamAsync()))
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();
                byte status = 0;
                switch (responseString)
                {
                    case "200":
                    case "101":
                        responseString = "200 : Success [" + responseString + "]";
                        status = 1;
                        break;
                    case "102":
                        responseString = "102 : Exception while saving bill details";
                        status = 0;
                        break;
                    case "103":
                        responseString = "103 : Unknown exceptions";
                        status = 0;
                        break;
                    case "100":
                        responseString = "100 : API credentials do not match";
                        status = 0;
                        break;
                }
                await LogSyncStatus(SalesModel.invoice_number, SalesModel.fiscal_year, JsonConvert.SerializeObject(SalesModel), status, responseString, isRealTime);
                return responseString == "200";
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    FunctionResponse res = JsonConvert.DeserializeObject<FunctionResponse>(Response);
                    if (res != null && res.result != null)
                        await LogSyncStatus(SalesModel.invoice_number, SalesModel.fiscal_year, JsonConvert.SerializeObject(SalesModel), 0, res.result.ToString());
                    else
                        await LogSyncStatus(SalesModel.invoice_number, SalesModel.fiscal_year, JsonConvert.SerializeObject(SalesModel), 0, Response);
                }
                else
                    await LogSyncStatus(SalesModel.invoice_number, SalesModel.fiscal_year, JsonConvert.SerializeObject(SalesModel), 0, ex.GetBaseException().Message);
                return false;
            }
            catch (Exception ex)
            {
                await LogSyncStatus(SalesModel.invoice_number, SalesModel.fiscal_year, JsonConvert.SerializeObject(SalesModel), 0, ex.GetBaseException().Message);
                return false;
            }
        }

        public static async Task<bool> SyncSalesReturnData(BillReturnViewModel SalesReturnModel, byte isRealTime = 0)
        {
            try
            {
                SalesReturnModel.username = username;
                SalesReturnModel.password = password;
                var request = (HttpWebRequest)WebRequest.Create(Url + "/billreturn");
                var data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(SalesReturnModel));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (var stream = (await request.GetRequestStreamAsync()))
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();
                byte status = 0;
                switch (responseString)
                {
                    case "200":
                    case "101":
                        responseString = "200 : Success [" + responseString + "]";
                        status = 1;
                        break;
                    case "102":
                        responseString = "102 : Exception while saving bill details";
                        status = 0;
                        break;
                    case "103":
                        responseString = "103 : Unknown exceptions";
                        status = 0;
                        break;
                    case "100":
                        responseString = "100 : API credentials do not match";
                        status = 0;
                        break;
                    case "105":
                        responseString = "105 : Bill does not exists";
                        status = 0;
                        break;
                }
                await LogSyncStatus(SalesReturnModel.credit_note_number, SalesReturnModel.fiscal_year, JsonConvert.SerializeObject(SalesReturnModel), status, responseString, isRealTime);
                return status == 1;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    FunctionResponse res = JsonConvert.DeserializeObject<FunctionResponse>(Response);
                    if (res != null)
                        await LogSyncStatus(SalesReturnModel.credit_note_number, SalesReturnModel.fiscal_year, JsonConvert.SerializeObject(SalesReturnModel), 0, res.result.ToString());
                    else
                        await LogSyncStatus(SalesReturnModel.credit_note_number, SalesReturnModel.fiscal_year, JsonConvert.SerializeObject(SalesReturnModel), 0, Response);
                }
                else
                    await LogSyncStatus(SalesReturnModel.credit_note_number, SalesReturnModel.fiscal_year, JsonConvert.SerializeObject(SalesReturnModel), 0, ex.GetBaseException().Message);
                return false;
            }
            catch (Exception ex)
            {
                await LogSyncStatus(SalesReturnModel.credit_note_number, SalesReturnModel.fiscal_year, JsonConvert.SerializeObject(SalesReturnModel), 0, ex.GetBaseException().Message);
                return false;
            }
        }
        static async Task LogSyncStatus(string vchrno, string fyname, string json_data, byte status, string return_code, byte isrealtime = 0)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DateTime SyncTime = conn.ExecuteScalar<DateTime>("SELECT GETDATE()");
                await conn.ExecuteAsync("UPDATE tblSyncLog SET STATUS = @status, RETURN_CODE = @return_code, JSON_DATA = @json_data, IsRealTime = @isrealtime, SYNC_DATE = @SyncDate, SYNC_TIME = @SyncTime  WHERE VCHRNO = @vchrno AND FYNAME = @fyname", new { vchrno, status, return_code, json_data, SyncDate = SyncTime.Date, SyncTime = SyncTime.ToString("hh:mm:ss tt"), isrealtime, fyname });
            }
        }
        internal static void LogSyncStatus(SqlTransaction tran, string vchrno, string fyname, string json_data = "", byte status = 0, string return_code = "", byte isrealtime = 0)
        {
            if (!string.IsNullOrEmpty(username))
                tran.Connection.Execute("INSERT INTO tblSyncLog (VCHRNO, FYNAME, SYNC_DATE, SYNC_TIME, JSON_DATA, STATUS, RETURN_CODE, IsRealTime) VALUES (@vchrno, @fyname, @SyncDate, @SyncTime, @json_data, @status, @return_code, @isrealtime)", new { vchrno, fyname, status, json_data, return_code, isrealtime, SyncDate = DateTime.Today, SyncTime = DateTime.Now.ToString("hh:mm:ss tt") }, tran);
        }
    }

    public class FunctionResponse
    {
        public string status { get; set; }
        public object result { get; set; }
        public string RefNo { get; set; }
    }
}
