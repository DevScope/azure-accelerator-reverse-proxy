namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using Microsoft.WindowsAzure.StorageClient;

    public class SyncStatusRow : TableServiceEntity
    {
        public SyncStatusRow()
        { 
        }

        public SyncStatusRow(string deploymentId, string roleInstanceId, string siteName)
            : base(deploymentId, roleInstanceId + ";" + siteName)
        {
            this.RoleInstanceId = roleInstanceId;
            this.SiteName = siteName;
        }

        public string RoleInstanceId { get; set; }

        public string SiteName { get; set; }

        public string Status { get; set; }

        public bool? IsOnline { get; set; }
    }
}