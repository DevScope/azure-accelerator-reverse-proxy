namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services
{
    using System;

    public interface ISyncService
    {
        void SyncForever(TimeSpan interval);
    }
}