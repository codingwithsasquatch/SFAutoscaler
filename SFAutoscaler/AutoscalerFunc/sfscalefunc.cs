using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace AutoscalerFunc
{
    public static class sfscalefunc
    {
        [FunctionName("sfscalefunc")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, 
            TraceWriter log)
        {
            log.Info(string.Format("sfscalefunc - {0}", DateTime.Now.ToLongTimeString()));
            var events = await req.Content.ReadAsAsync<IEnumerable<GridEvent<object>>>();
            log.Info(events.Count().ToString());
            var client = new HttpClient { BaseAddress = new Uri("http://a12af200.ngrok.io/api/scale") };

            var json = JsonConvert.SerializeObject(events);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(string.Empty, stringContent);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        public class GridEvent<T> where T : class
        {

            public string Id { get; set; }


            public string Subject { get; set; }


            public string EventType { get; set; }


            public T Data { get; set; }


            public DateTime EventTime { get; set; }

        }
    }
}
