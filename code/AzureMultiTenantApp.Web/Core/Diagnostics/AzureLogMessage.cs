namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics
{
    using System;
    using Microsoft.WindowsAzure.StorageClient;

    public sealed class AzureLogMessage : TableServiceEntity
    {
        public string ActivityId { get; set; }

        public DateTime LogTimestamp { get; set; }

        public string Level { get; set; }

        public string Message { get; set; }

        public string RoleInstanceId { get; set; }
    }
}