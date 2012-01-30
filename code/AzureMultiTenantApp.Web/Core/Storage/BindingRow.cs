namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using Microsoft.WindowsAzure.StorageClient;

    public class BindingRow : TableServiceEntity
    {
        public BindingRow()
            : this(Guid.NewGuid())
        { 
        }

        public BindingRow(Guid id)
            : base("binding", id.ToString())
        {
        }

        public Guid WebSiteId { get; set; }

        public string Protocol { get; set; }

        public string IpAddress { get; set; }

        public int Port { get; set; }

        public string HostName { get; set; }

        public Guid? CertificateId { get; set; }
    }
}