namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services
{
    using System.Collections.Generic;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;

    public interface IIISManager
    {
        void UpdateSites(IEnumerable<WebSite> sites);
    }
}