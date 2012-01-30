namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System.Collections.Generic;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;

    public interface ISyncStatusRepository
    {
        void RemoveWebSiteStatus(string webSiteName);
        
        void UpdateStatus(SyncStatus syncStatus);

        IEnumerable<SyncStatus> RetrieveSyncStatus(string webSiteName);

        IEnumerable<SyncStatus> RetrieveSyncStatusByInstanceId(string roleInstanceId);
    }
}