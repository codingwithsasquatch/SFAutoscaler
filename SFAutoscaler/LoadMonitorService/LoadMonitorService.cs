using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using EventGrid.Helper;
using EventGrid.Helper.Events;

namespace LoadMonitorService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class LoadMonitorService : StatefulService
    {
        private const string TopicEndpoint = "https://sfautoscaler.westus2-1.eventgrid.azure.net/api/events";
        private const string TopicKey = "tWtFrgaD70han/OQKCrhawJuYmvEG92Wr02RORbJ1Fc=";
        private static FabricClient fc = new FabricClient();

        public LoadMonitorService(StatefulServiceContext context)
            : base(context)
        { }


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (!cancellationToken.IsCancellationRequested)
            {
                var loadInfo = (await fc.QueryManager.GetClusterLoadInformationAsync()).LoadMetricInformationList;
                
                //generate the new message ID
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter", LockMode.Update);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    var messageId = await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.

                    var loadInfoEvents = new List<GridEvent<LoadInfo>>
                    {
                            new GridEvent<LoadInfo>()
                            {
                                Data =
                                    new LoadInfo()
                                    {
                                        ClusterLoadInfo = loadInfo
                                    },
                                Subject = "clusterevent/loadinfo",
                                EventType = "loadinfo",
                                EventTime = DateTime.UtcNow,
                                Id = messageId.ToString()
                            }
                    };

                    await Utils.SendEvent(TopicEndpoint, TopicKey, loadInfoEvents);

                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(5));

            }
        }
    }
}
