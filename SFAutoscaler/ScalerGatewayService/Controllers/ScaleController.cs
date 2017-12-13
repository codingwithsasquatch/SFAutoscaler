using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using System.Net.Http;
using System.Fabric.Query;

namespace ScalerGatewayService.Controllers
{
    [Route("api/[controller]")]
    public class ScaleController : Controller
    {
        private FabricClient _fabricClient;
        private HttpClient _httpClient;

        public ScaleController(FabricClient fabricClient, HttpClient httpClient)
        {
            _fabricClient = fabricClient;
            _httpClient = httpClient;
        }

        // POST api/values
        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody]dynamic value)
        {
            ServicePartitionList partitionList = await _fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/SFAutoscalerApplication/ScalerService"));

            foreach (Partition partition in partitionList)
            {
                long partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;

                string proxyUrl = $"http://localhost:{19081}/SFAutoscalerApplication/ScalerService/api/loadinfo?PartitionKind={partition.PartitionInformation.Kind}&PartitionKey={partitionKey}";

                HttpResponseMessage response = await _httpClient.PostAsync(proxyUrl, new StringContent(value));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    // if one partition returns a failure, you can either fail the entire request or skip that partition.
                    return new HttpResponseMessage(response.StatusCode);
                }
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
