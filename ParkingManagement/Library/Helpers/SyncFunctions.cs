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
        const string Url = "http://www.imsnepal.com:8080/testservice/api";
        public static string username { get; private set; }
        static string password;

        static SyncFunctions()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                username = conn.ExecuteScalar<string>("SELECT ISNULL(IrdApiUser,'') FROM tblSetting");
                password = conn.ExecuteScalar<string>("SELECT ISNULL(IrdApiPassword,'') FROM tblSetting");
            }
        }
        public static async Task<bool> SyncSalesData(BillViewModel SalesModel)
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

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

                await LogSyncStatus(SalesModel.invoice_number, JsonConvert.SerializeObject(SalesModel), (byte)((responseString == "200") ? 1 : 0), responseString);

                return responseString == "200";
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    FunctionResponse res = JsonConvert.DeserializeObject<FunctionResponse>(Response);
                    if (res != null)
                        await LogSyncStatus(SalesModel.invoice_number, JsonConvert.SerializeObject(SalesModel), 1, res.result.ToString());
                    else
                        await LogSyncStatus(SalesModel.invoice_number, JsonConvert.SerializeObject(SalesModel), 1, Response);
                }
                else
                    await LogSyncStatus(SalesModel.invoice_number, JsonConvert.SerializeObject(SalesModel), 1, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> SyncSalesReturnData(BillReturnViewModel SalesReturnModel)
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

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)await request.GetResponseAsync();
                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

                await LogSyncStatus(SalesReturnModel.credit_note_number, JsonConvert.SerializeObject(SalesReturnModel), (byte)((responseString == "200") ? 1 : 0), responseString);

                return responseString == "200";
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                    FunctionResponse res = JsonConvert.DeserializeObject<FunctionResponse>(Response);
                    if (res != null)
                        await LogSyncStatus(SalesReturnModel.credit_note_number, JsonConvert.SerializeObject(SalesReturnModel), 1, res.result.ToString());
                    else
                        await LogSyncStatus(SalesReturnModel.credit_note_number, JsonConvert.SerializeObject(SalesReturnModel), 1, Response);
                }
                else
                    await LogSyncStatus(SalesReturnModel.credit_note_number, JsonConvert.SerializeObject(SalesReturnModel), 1, ex.Message);
                return false;
            }
        }
        static async Task LogSyncStatus(string vchrno, string json_data, byte status, string return_code)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                await conn.ExecuteAsync("INSERT INTO tblSyncLog (VCHRNO, SYNC_DATE, SYNC_TIME, JSON_DATA, STATUS, RETURN_CODE) VALUES (@vchrno, CONVERT(DATE, GETDATE()), CONVERT(VARCHAR, GETDATE(), 108), @json_data, @status, @return_code)", new { vchrno, status, json_data, return_code });
            }
        }
    }

    public class FunctionResponse
    {
        public string status { get; set; }
        public object result { get; set; }
        public string RefNo { get; set; }
    }
}
