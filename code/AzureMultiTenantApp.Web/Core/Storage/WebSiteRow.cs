namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using Microsoft.WindowsAzure.StorageClient;

    public class WebSiteRow : TableServiceEntity
    {
        public WebSiteRow()
            : this(Guid.NewGuid())
        { 
        }

        public WebSiteRow(Guid id)
            : base("website", id.ToString())
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool? EnableCDNChildApplication { get; set; }

        public bool? EnableTestChildApplication { get; set; }
    }
}