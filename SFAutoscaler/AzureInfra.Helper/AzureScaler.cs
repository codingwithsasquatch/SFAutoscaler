using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;

using System.Threading;

namespace AzureInfra.Helper
{
    public class AzureScaler
    {
        private ServicePrincipalLoginInformation spli = null;
        private AzureCredentials creds = null;
        private IAzure azureClient = null;
        private int DefaultMaxNodeCountPerVmss = 100;
        private TimeSpan DefaultCheckbackTimespan = TimeSpan.FromMinutes(1);

        public AzureScaler(string AzureClientId, string AzureClientKey, string TenantId)
        {
            this.spli = new ServicePrincipalLoginInformation
            {
                ClientId = AzureClientId,
                ClientSecret = AzureClientKey
            };

            this.creds = new AzureCredentials(spli, TenantId, AzureEnvironment.AzureGlobalCloud);

            this.azureClient = Azure.Authenticate(this.creds).WithDefaultSubscription();

        }

        public Task AddNodesAsync(string ScaleSetId, int NodesToAdd, CancellationToken ct)
        {
            var scaleSet = this.azureClient.VirtualMachineScaleSets.GetById(ScaleSetId);
            var newCapacity = (int)Math.Min(this.DefaultMaxNodeCountPerVmss, scaleSet.Capacity + NodesToAdd);
            scaleSet.Update().WithCapacity(newCapacity).Apply();
            return Task.FromResult(true);
        }

        public async Task VerifyScaleAsync(string ScaleSetId, int TargetScale, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(this.DefaultCheckbackTimespan);
                if (this.azureClient.VirtualMachineScaleSets.GetById(ScaleSetId).Capacity == TargetScale)
                {
                    return;
                }
            }
        }
    }
}
