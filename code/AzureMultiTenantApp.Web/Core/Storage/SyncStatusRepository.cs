namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class SyncStatusRepository : ISyncStatusRepository
    {
        private readonly IAzureTable<SyncStatusRow> table;

        public SyncStatusRepository()
            : this("DataConnectionString")
        {
        }

        public SyncStatusRepository(string settingName)
            : this(CloudStorageAccount.FromConfigurationSetting(settingName), "WebSitesSyncStatus")
        {
        }

        public SyncStatusRepository(CloudStorageAccount account)
            : this(account, "WebSitesSyncStatus")
        {
        }

        public SyncStatusRepository(CloudStorageAccount account, string tableName)
            : this(new AzureTable<SyncStatusRow>(account, tableName))
        { 
        }

        public SyncStatusRepository(IAzureTable<SyncStatusRow> table)
        {
            this.table = table;
            this.table.CreateIfNotExist();
        }

        public void RemoveWebSiteStatus(string webSiteName)
        {
            var webSiteStatus = this.RetrieveSyncStatus(webSiteName);
            if (webSiteStatus != null && webSiteStatus.Count() > 0)
            {
                this.table.DeleteEntity(webSiteStatus.Select(s => s.ToRow()));
            }
        }

        public void UpdateStatus(SyncStatus syncStatus)
        {
            this.table.AddOrUpdateEntity(syncStatus.ToRow());
        }

        public IEnumerable<SyncStatus> RetrieveSyncStatus(string webSiteName)
        {
            return this.table.Query
                .Where(
                    s => 
                        s.PartitionKey.Equals(RoleEnvironment.DeploymentId, StringComparison.OrdinalIgnoreCase) &&
                        s.SiteName.Equals(webSiteName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .Select(s => s.ToModel());
        }

        public IEnumerable<SyncStatus> RetrieveSyncStatusByInstanceId(string roleInstanceId)
        {
            return this.table.Query
                .Where(
                    s =>
                        s.PartitionKey.Equals(RoleEnvironment.DeploymentId, StringComparison.OrdinalIgnoreCase) &&
                        s.RoleInstanceId.Equals(roleInstanceId, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .Select(s => s.ToModel());
        }
    }
}