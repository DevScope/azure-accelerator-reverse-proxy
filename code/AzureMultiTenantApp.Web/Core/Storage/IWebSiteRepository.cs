namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;

    public interface IWebSiteRepository
    {
        void CreateWebSite(WebSite webSite);

        void CreateWebSiteWithBinding(WebSite webSite, Binding binding);

        void AddBindingToWebSite(Guid webSiteId, Binding binding);

        void RemoveBinding(Guid bindingId);

        void EditBinding(Binding binding);

        void UpdateWebSite(WebSite webSite);

        void UpdateBinding(Binding binding);

        void RemoveWebSite(Guid webSiteId);

        WebSite RetrieveWebSite(Guid webSiteId);

        Binding RetrieveBinding(Guid bindingId);

        WebSite RetrieveWebSiteWithBindings(Guid webSiteId);

        WebSite RetrieveWebSiteWithBindingsAndCertificates(Guid webSiteId, ICertificateRepository certificateRepository);

        IEnumerable<Binding> RetrieveWebSiteBindings(Guid webSiteId);

        IEnumerable<Binding> RetrieveCertificateBindings(Guid certificateId);

        IEnumerable<Binding> RetrieveBindingsForPort(int port);

        void AddBindingToWebSite(WebSite webSite, Binding binding);

        IEnumerable<WebSite> RetrieveWebSites();

        IEnumerable<WebSite> RetrieveWebSitesWithBindings();

        IEnumerable<WebSite> RetrieveWebSitesWithBindingsAndCertificates(ICertificateRepository certificateRepository);
    }
}