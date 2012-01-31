namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using Microsoft.WindowsAzure.StorageClient;

    public class CertificateRow : TableServiceEntity
    {
        public CertificateRow()
            : this(Guid.NewGuid())
        { 
        }

        public CertificateRow(Guid id)
            : base("certificate", id.ToString())
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Password { get; set; }
    }
}