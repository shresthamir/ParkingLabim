using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParkingManagement.ViewModel
{
    public class DeductionViewModel
    {
        protected string MessageBoxCaption;

        public DeductionViewModel()
        {

        }
    //    bool ValidateKKFC(string Url1, string Url2, string ClientId, string ClientSecretKey, string CardNumber, string TransactionId)
    //    {
    //        try
    //        {
    //            var PinRequest = (HttpWebRequest)WebRequest.Create(Url1);
    //            var ClinetInfo = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new { ClientId, ClientSecretKey }));
    //            PinRequest.Method = "POST";
    //            PinRequest.ContentType = "application/json";
    //            PinRequest.ContentLength = ClinetInfo.Length;

    //            using (var stream = (PinRequest.GetRequestStream()))
    //            {
    //                stream.Write(ClinetInfo, 0, ClinetInfo.Length);
    //            }
    //            var response = (HttpWebResponse)PinRequest.GetResponse();
    //            var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
    //            if (result.GetValue("status").ToString() != "ok")
    //            {
    //                MessageBox.Show(result.GetValue("message").ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
    //                return false;
    //            }
    //            var PinNumber = result.GetValue("result").ToString();

    //            var PaymentRequest = (HttpWebRequest)WebRequest.Create(Url2);
    //            var PaymentInfo = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new
    //            {
    //                ClientId,
    //                ClientSecretKey,
    //                Amount = POUT.CashAmount,
    //                CardNumber,
    //                PinNumber,
    //                TransactionId,
    //                Description = POUT.BILLTO?.ToString(),
    //                TransactionNumber = TransactionId
    //            }));
    //            PaymentRequest.Method = "POST";
    //            PaymentRequest.ContentType = "application/json";
    //            PaymentRequest.ContentLength = PaymentInfo.Length;

    //            using (var stream = (PaymentRequest.GetRequestStream()))
    //            {
    //                stream.Write(PaymentInfo, 0, PaymentInfo.Length);
    //            }
    //            var PaymentResponse = (HttpWebResponse)PaymentRequest.GetResponse();
    //            var ResponseMessage = new System.IO.StreamReader(PaymentResponse.GetResponseStream()).ReadToEnd();
    //            result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(ResponseMessage);
    //            if (result.GetValue("status").ToString() != "ok")
    //            {
    //                MessageBox.Show(result.GetValue("message").ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
    //                return false;
    //            }
    //            POUT.STAFF_BARCODE = PinNumber + ":" + result.GetValue("result").ToString();
    //            return true;
    //        }
    //        catch (WebException ex)
    //        {
    //            if (ex.Status == WebExceptionStatus.ProtocolError)
    //            {
    //                string Response = new System.IO.StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
    //                var res = JsonConvert.DeserializeObject<dynamic>(Response);
    //                MessageBox.Show(res.Message.ToString(), MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

    //            }
    //            else
    //            {
    //                MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
    //            }

    //            return false;
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show(ex.GetBaseException().Message, MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
    //            return false;
    //        }
    //    }
    }
}
