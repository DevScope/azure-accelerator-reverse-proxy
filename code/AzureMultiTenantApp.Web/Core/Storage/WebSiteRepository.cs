namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;

    public class WebSiteRepository : IWebSiteRepository
    {
        private readonly AzureTable<WebSiteRow> webSiteTable;
        private readonly AzureTable<BindingRow> bindingTable;

        public WebSiteRepository()
            : this("DataConnectionString")
        {
        }

        public WebSiteRepository(string settingName)
            : this(CloudStorageAccount.FromConfigurationSetting(settingName), "WebSites", "Bindings")
        {
        }

        public WebSiteRepository(CloudStorageAccount account)
            : this(account, "WebSites", "Bindings")
        {
        }

        public WebSiteRepository(CloudStorageAccount account, string webSiteTableName, string bindingTableName)
            : this(new AzureTable<WebSiteRow>(account, webSiteTableName), new AzureTable<BindingRow>(account, bindingTableName))
        {
        }

        public WebSiteRepository(AzureTable<WebSiteRow> webSiteTable, AzureTable<BindingRow> bindingTable)
        {
            this.webSiteTable = webSiteTable;
            this.bindingTable = bindingTable;

            this.webSiteTable.CreateIfNotExist();
            this.bindingTable.CreateIfNotExist();
        }

        public void CreateWebSite(WebSite webSite)
        {
            this.webSiteTable.AddEntity(webSite.ToRow());
        }

        public void CreateWebSiteWithBinding(WebSite webSite, Binding binding)
        {
            binding.WebSiteId = webSite.Id;

            this.webSiteTable.AddEntity(webSite.ToRow());
            this.bindingTable.AddEntity(binding.ToRow());
        }

        public void AddBindingToWebSite(Guid webSiteId, Binding binding)
        {
            binding.WebSiteId = webSiteId;
            this.bindingTable.AddEntity(binding.ToRow());
        }

        public void RemoveBinding(Guid bindingId)
        {
            string key = bindingId.ToString();
            this.bindingTable.DeleteEntity(this.bindingTable.Query.Where(b => b.RowKey == key));
        }

        public void EditBinding(Binding binding)
        {
            this.bindingTable.AddOrUpdateEntity(binding.ToRow());
        }

        public void UpdateWebSite(WebSite webSite)
        {
            this.webSiteTable.AddOrUpdateEntity(webSite.ToRow());
        }

        public void UpdateBinding(Binding binding)
        {
            this.bindingTable.AddOrUpdateEntity(binding.ToRow());
        }

        public void RemoveWebSite(Guid webSiteId)
        {
            string key = webSiteId.ToString();

            var websites = this.webSiteTable.Query.Where(ws => ws.RowKey == key);
            var bindings = this.bindingTable.Query.Where(b => b.WebSiteId == webSiteId);

            this.webSiteTable.DeleteEntity(websites);
            this.bindingTable.DeleteEntity(bindings);
        }

        public WebSite RetrieveWebSite(Guid webSiteId)
        {
            string key = webSiteId.ToString();

            return this.webSiteTable.Query.Where(ws => ws.RowKey == key).FirstOrDefault().ToModel();
        }

        public Binding RetrieveBinding(Guid bindingId)
        {
            string key = bindingId.ToString();

            return this.bindingTable.Query.Where(b => b.RowKey == key).FirstOrDefault().ToModel();
        }

        public WebSite RetrieveWebSiteWithBindings(Guid webSiteId)
        {
            WebSite website = this.RetrieveWebSite(webSiteId);

            website.Bindings = this.RetrieveWebSiteBindings(webSiteId);

            return website;
        }

        public WebSite RetrieveWebSiteWithBindingsAndCertificates(Guid webSiteId, ICertificateRepository certificateRepository)
        {
            WebSite website = this.RetrieveWebSiteWithBindings(webSiteId);

            if (website.Bindings != null) 
            {
                foreach (var binding in website.Bindings)
                {
                    certificateRepository.RetrieveCertificateForBinding(binding);
                }
            }

            return website;
        }

        public IEnumerable<Binding> RetrieveWebSiteBindings(Guid webSiteId)
        {
            return this.bindingTable.Query.Where(b => b.WebSiteId == webSiteId).ToList().Select(b => b.ToModel()).ToList();
        }

        public IEnumerable<Binding> RetrieveCertificateBindings(Guid certificateId)
        {
            var bindings = this.bindingTable.Query.Where(b => b.CertificateId == certificateId).ToList().Select(b => b.ToModel()).ToList();

            var sites = new Dictionary<Guid, WebSite>();

            foreach (var binding in bindings) 
            {
                if (!sites.ContainsKey(binding.WebSiteId))
                {
                    sites[binding.WebSiteId] = this.RetrieveWebSite(binding.WebSiteId);
                }

                binding.WebSite = sites[binding.WebSiteId];
            }

            return bindings;
        }

        public IEnumerable<Binding> RetrieveBindingsForPort(int port)
        {
            return this.bindingTable.Query.Where(b => b.Port == port).ToList().Select(b => b.ToModel()).ToList();
        }

        public void AddBindingToWebSite(WebSite webSite, Binding binding)
        {
            binding.WebSiteId = webSite.Id;
            this.bindingTable.AddEntity(binding.ToRow());
        }

        public IEnumerable<WebSite> RetrieveWebSites()
        {
            return this.webSiteTable.Query.ToList().OrderBy(t => t.Name).Select(ws => ws.ToModel()).ToList();
        }

        public IEnumerable<WebSite> RetrieveWebSitesWithBindings()
        {
            var sites = this.RetrieveWebSites();

            foreach (var site in sites)
            {
                site.Bindings = this.RetrieveWebSiteBindings(site.Id);
            }

            return sites;
        }

        public IEnumerable<WebSite> RetrieveWebSitesWithBindingsAndCertificates(ICertificateRepository certificateRepository)
        {
            var sites = this.RetrieveWebSites();

            foreach (var site in sites)
            {
                site.Bindings = this.RetrieveWebSiteBindings(site.Id);

                foreach (var binding in site.Bindings)
                {
                    certificateRepository.RetrieveCertificateForBinding(binding);
                }
            }

            return sites;
        }
    }
}