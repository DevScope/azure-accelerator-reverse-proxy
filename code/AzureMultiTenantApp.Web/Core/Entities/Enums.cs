namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;

    public enum SyncInstanceStatus
    {
        NotCreated,
        Created,
        Deployed,
        Error
    }
}