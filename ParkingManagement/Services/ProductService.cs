using Newtonsoft.Json;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Services
{
    public class ProductService
    {
        public static async Task<FunctionResponse> CreateProduct(string desca)
        {
            try
            {
                FunctionResponse functionResponse = new Library.Helpers.FunctionResponse();

                var JsonObject = JsonConvert.SerializeObject(desca);

                string ContentType = "application/json";
                string url = GlobalClass.ServerIpAddress + "/api/MenuMapping/CreateProduct";
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
        public static async Task<FunctionResponse> GetMcodeByDesca(string desca)
        {
            try
            {
                FunctionResponse functionResponse = new Library.Helpers.FunctionResponse();

                var JsonObject = JsonConvert.SerializeObject(desca);

                string url = GlobalClass.ServerIpAddress + "/api/MenuMapping/GetMcodeByDesca/" + desca;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(url);
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
        public static async Task<FunctionResponse> CheckIfMenuCodeExists(int prodid, string desca)
        {
            try
            {
                FunctionResponse functionResponse = new Library.Helpers.FunctionResponse();
                var JsonObject = JsonConvert.SerializeObject(new { prodid, desca });
                string ContentType = "application/json";

                string url = GlobalClass.ServerIpAddress + "/api/MenuMapping/CheckIfMenuCodeExists";
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

    }
}
