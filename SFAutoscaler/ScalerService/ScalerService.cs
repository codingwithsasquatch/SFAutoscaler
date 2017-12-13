using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Fabric.Query;

namespace ScalerService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class ScalerService : Microsoft.ServiceFabric.Services.Runtime.StatefulService
    {
        private static FabricClient fc = new FabricClient();

        public ScalerService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(HandleEvents(cancellationToken), ScaleTracker(cancellationToken));
        }

        private async Task HandleEvents(CancellationToken cancellationToken)
        {
            long maxProcessed = -1;
            long currentProcessed = -1;

            var events = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, IList<LoadMetricInformation>>>("Events");
            var inbox = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<long>>("Inbox");
            var progress = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, ScaleOperationStatus>>("Progress");

            while (!cancellationToken.IsCancellationRequested)
            {
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    var result = await inbox.TryDequeueAsync(tx);
                    if (result.HasValue)
                    {
                        currentProcessed = result.Value;
                    }

                    if (currentProcessed <= maxProcessed) //this is a dupe or stale, remove the data
                    {
                        await events.TryRemoveAsync(tx, currentProcessed);
                    }
                    else
                    {
                        var data = await events.TryGetValueAsync(tx, currentProcessed);

                        if (data.HasValue)
                        {
                            foreach (var metricInfo in data.Value)
                            {
                                if (((metricInfo.ClusterLoad / metricInfo.ClusterCapacity * 100) > 50) || (metricInfo.IsClusterCapacityViolation))
                                {
                                    await progress.AddAsync(tx, currentProcessed, ScaleOperationStatus.NotStarted);
                                }
                            }
                        }
                    }

                    await tx.CommitAsync();
                }

                return;
            }
        }

        private async Task ScaleTracker(CancellationToken cancellationToken)
        {
            var progress = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, ScaleOperationStatus>>("Progress");
            IList<KeyValuePair<long, ScaleOperationStatus>> operations = new List<KeyValuePair<long, ScaleOperationStatus>>();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await progress.CreateEnumerableAsync(tx, EnumerationMode.Unordered)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    if (enumerator.Current.Value == ScaleOperationStatus.NotStarted)
                    {
                        operations.Add(enumerator.Current);
                    }
                }

                await tx.CommitAsync();
            }

            foreach (var operation in operations)
            {
                if (operation.Value == ScaleOperationStatus.NotStarted)
                {
                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        //kickoff scale operation then
                        var result = await progress.TryUpdateAsync(tx, operation.Key, ScaleOperationStatus.InProgress, ScaleOperationStatus.NotStarted);

                        await tx.CommitAsync();
                    }
                }
            }
        }
            

        private enum ScaleOperationStatus
        {
            NotStarted,
            InProgress,
            Done
        }
    }
}
