using Newtonsoft.Json;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FunctionResponse = ParkingManagement.Library.Helpers.FunctionResponse;

namespace ParkingManagement.Services
{
    public class BillingService
    {
        public static async Task<FunctionResponse> SaveBill(BillMain billMain)
        {
            try
            {
                FunctionResponse functionResponse = new Library.Helpers.FunctionResponse();

                var JsonObject = JsonConvert.SerializeObject(billMain);

                string ContentType = "application/json"; // or application/xml
                string url = GlobalClass.ServerIpAddress + "/api/SaveBill";
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.PostAsync(url, new StringContent(JsonObject.ToString(), Encoding.UTF8, ContentType));
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FunctionResponse>(json);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            };
        }
        public static async Task<bool> CheckIfparkingSalesAlreadyExist(string mcode)
        {
            try
            {
                //string ContentType = "application/json"; // or application/xml
                string url = GlobalClass.ServerIpAddress + "/api/CheckTransactionOfTheDay/"+mcode;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<bool>(json);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            };
        }

        public static async Task<bool> CheckIfMembershipSalesAlreadyExist(string mcode)
        {
            try
            {
                //string ContentType = "application/json"; // or application/xml
                string url = GlobalClass.ServerIpAddress + "/api/CheckIfMembershipSalesAlreadyExist/" + mcode;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<bool>(json);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            };
        }
    }
}
