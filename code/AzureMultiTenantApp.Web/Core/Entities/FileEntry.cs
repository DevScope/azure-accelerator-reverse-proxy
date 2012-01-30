namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;

    public class FileEntry
    {
        public DateTime LocalLastModified { get; set; }

        public DateTime CloudLastModified { get; set; }

        public bool IsDirectory { get; set; }

        public DateTime LastDeployed { get; set; }
    }
}