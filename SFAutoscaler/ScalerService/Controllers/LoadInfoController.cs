using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Net;
using System.Fabric.Query;

namespace ScalerService.Controllers
{
    [Route("api/[controller]")]
    public class LoadInfoController : Controller
    {
        IReliableStateManager stateManager;

        public LoadInfoController(IReliableStateManager ReliableStateManager)
        {
            this.stateManager = ReliableStateManager;
        }

        // POST api/LoadInfo
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            var events = await stateManager.GetOrAddAsync<IReliableDictionary<long, IList<LoadMetricInformation>>>("Events");
            var inbox = await stateManager.GetOrAddAsync<IReliableConcurrentQueue<long>>("Inbox");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await inbox.EnqueueAsync(tx, 0);
                await events.AddAsync(tx, 0, new List<LoadMetricInformation>());
                await tx.CommitAsync();
            }
        }
    }
}
