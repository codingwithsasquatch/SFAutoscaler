﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using EventGrid.Helper;
using EventGrid.Helper.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoscalerBot.Controllers
{
    public class AutoscaleController : ApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            var result = await Request.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(result);
            if (events[0].EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationCode = events[0].Data["validationCode"];
                var validationResponse = JsonConvert.SerializeObject(new { validationResponse = validationCode });
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(validationResponse)
                };
            }
            else if(events[0].EventType == "loadinfo")
            {
                var loadInfo = JsonConvert.DeserializeObject<List<GridEvent<LoadInfo>>>(result);
            }
            else if (events[0].EventType == "scaleinfo")
            {
                var scaleInfo = JsonConvert.DeserializeObject<List<GridEvent<ScaleInfo>>>(result);
            }           


            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}