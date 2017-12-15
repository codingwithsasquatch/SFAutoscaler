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
using AzureInfra.Helper;

namespace ScalerService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class ScalerService : Microsoft.ServiceFabric.Services.Runtime.StatefulService
    {
        private static FabricClient fc = new FabricClient();
        private static AzureScaler azureScaler = null;

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
                    maxProcessed = currentProcessed;
                }

                return;
            }
        }

        private async Task ScaleTracker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

                var progress = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, ScaleOperationStatus>>("Progress");

                var completedOperations = new List<long>();

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    var enumerator = (await progress.CreateEnumerableAsync(tx, EnumerationMode.Unordered)).GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        if (enumerator.Current.Value == ScaleOperationStatus.NotStarted)
                        {
                            await Task.Run(() => ScaleoutTask(enumerator.Current.Key, cancellationToken));
                        }
                        else if(enumerator.Current.Value == ScaleOperationStatus.Done)
                        {
                            await Task.Run(() => CompleteScaleOperation(enumerator.Current.Key, cancellationToken));
                        }
                    }

                    await tx.CommitAsync();
                }
            }
        }

        private async Task ScaleoutTask(long eventId, CancellationToken ct)
        {
            var events = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, IList<LoadMetricInformation>>>("Events");
            var progress = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, ScaleOperationStatus>>("Progress");

            //theoretically figure out which vmss to scale based on the actual metric info
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await progress.SetAsync(tx, eventId, ScaleOperationStatus.InProgress);
                await azureScaler.AddNodesAsync("", 1, ct);
                await tx.CommitAsync();
            }
        }

        private async Task CompleteScaleOperation(long eventId, CancellationToken ct)
        {
            var events = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, IList<LoadMetricInformation>>>("Events");
            var progress = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, ScaleOperationStatus>>("Progress");

            //theoretically figure out which vmss to scale based on the actual metric info
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await progress.SetAsync(tx, eventId, ScaleOperationStatus.Done);
                await azureScaler.VerifyScaleAsync("", 1, ct); //figure out how to smuggle the intended target here
                await tx.CommitAsync();
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
