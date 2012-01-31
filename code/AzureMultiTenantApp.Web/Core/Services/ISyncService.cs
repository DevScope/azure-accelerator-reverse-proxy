namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services
{
    using System;

    public interface ISyncService
    {
        void Start();
        void UpdateAllSitesSyncStatus(string roleInstanceId, bool isOnline);
        void SyncForever(TimeSpan interval);
    }
}