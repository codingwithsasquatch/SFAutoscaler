using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace EventGrid.Helper
{
    public static class Utils
    {
        public static async Task SendEvent(string topicEndpoint, string topicKey, object data)
        {
            // Create a SAS token for the call to the event grid. We can do this with 
            // the SAS key as well but wanted to show an alternative.
            var sas = CreateEventGridSasToken(topicEndpoint, DateTime.Now.AddDays(1), topicKey);

            // Instantiate an instance of the HTTP client with the 
            // event grid topic endpoint.
            var client = new HttpClient { BaseAddress = new Uri(topicEndpoint) };

            // Configure the request headers with the content type
            // and SAS token needed to make the request.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("aeg-sas-token", sas);

            // Serialize the data
            var json = JsonConvert.SerializeObject(data);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Publish grid event
            var response = await client.PostAsync(string.Empty, stringContent);
        }

        public static string CreateEventGridSasToken(string resourcePath, DateTime expirationUtc, string topicKey)
        {
            const char resource = 'r';
            const char expiration = 'e';
            const char signature = 's';

            // Encode the topic resource path and expiration parameters
            var encodedResource = HttpUtility.UrlEncode(resourcePath);
            var encodedExpirationUtc = HttpUtility.UrlEncode(expirationUtc.ToString(CultureInfo.InvariantCulture));

            // Format the unsigned SAS token
            string unsignedSas = $"{resource}={encodedResource}&{expiration}={encodedExpirationUtc}";

            // Create an HMCASHA256 policy with the topic key
            using (var hmac = new HMACSHA256(Convert.FromBase64String(topicKey)))
            {
                // Encode the signature and create the fully signed URL with the
                // appropriate parameters.
                var bytes = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedSas)));
                var encodedSignature = HttpUtility.UrlEncode(bytes);
                var signedSas = $"{unsignedSas}&{signature}={encodedSignature}";

                return signedSas;
            }
        }
    }
}
