namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Tests.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;

    [TestClass]
    public class WebSiteRepositoryTests
    {
        private WebSiteRepository repository;
        private AzureTable<WebSiteRow> webSiteTable;
        private AzureTable<BindingRow> bindingTable;

        [TestInitialize]
        public void Setup()
        {
            this.webSiteTable = new AzureTable<WebSiteRow>(CloudStorageAccount.DevelopmentStorageAccount, "WebSitesTest");
            this.bindingTable = new AzureTable<BindingRow>(CloudStorageAccount.DevelopmentStorageAccount, "BindingsTest");
            this.webSiteTable.CreateIfNotExist();
            this.bindingTable.CreateIfNotExist();
            this.repository = new WebSiteRepository(this.webSiteTable, this.bindingTable);
        }

        [TestMethod]
        public void CreateNewWebSite()
        {
            var site = new WebSite()
            {
                Name = "Test Web Site",
                Description = "Description Test Web Site"
            };

            this.repository.CreateWebSite(site);

            string id = site.Id.ToString();

            WebSiteRow newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();

            Assert.IsNotNull(newsite);
            this.webSiteTable.DeleteEntity(newsite);
        }

        [TestMethod]
        public void CreateNewWebSiteWithInitialBinding()
        {
            WebSiteRow newsite = null;
            BindingRow newbinding = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(1);

                Binding binding = site.Bindings.First();

                string id = site.Id.ToString();
                string idb = binding.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();

                Assert.IsNotNull(newsite);
                Assert.AreEqual(site.Name, newsite.Name);
                Assert.AreEqual(site.Description, newsite.Description);

                newbinding = this.bindingTable.Query.Where(b => b.RowKey == idb).FirstOrDefault();

                Assert.IsNotNull(newbinding);
                Assert.AreEqual(binding.WebSiteId, newbinding.WebSiteId);
                Assert.AreEqual(binding.IpAddress, newbinding.IpAddress);
                Assert.AreEqual(binding.HostName, newbinding.HostName);
                Assert.AreEqual(binding.Port, newbinding.Port);
                Assert.AreEqual(binding.Protocol, newbinding.Protocol);

                var sites = this.repository.RetrieveWebSitesWithBindings();

                Assert.IsTrue(sites.Any(s => s.Id == site.Id && s.Bindings != null && s.Bindings.Count() == 1));
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbinding != null)
                {
                    this.bindingTable.DeleteEntity(newbinding);
                }
            }
        }

        [TestMethod]
        public void CreateNewWebSiteWithManyBindings()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(10);

                string id = site.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();

                Assert.IsNotNull(newsite);
                Assert.AreEqual(site.Name, newsite.Name);
                Assert.AreEqual(site.Description, newsite.Description);

                newbindings = this.bindingTable.Query.Where(b => b.WebSiteId == site.Id);

                Assert.IsNotNull(newbindings);
                Assert.AreEqual(10, newbindings.Count());

                var siteb = this.repository.RetrieveWebSiteWithBindings(site.Id);

                Assert.IsNotNull(siteb);
                Assert.IsNotNull(siteb.Bindings);
                Assert.AreEqual(10, siteb.Bindings.Count());
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void RemoveWebSite()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(10);

                string id = site.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                newbindings = this.bindingTable.Query.Where(b => b.WebSiteId == site.Id);

                this.repository.RemoveWebSite(site.Id);

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                newbindings = this.bindingTable.Query.Where(b => b.WebSiteId == site.Id);

                Assert.IsNull(newsite);
                Assert.IsNotNull(newbindings);
                Assert.AreEqual(0, newbindings.Count());
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void CreateAndRemoveWebSiteWithInitialBinding()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(1);
                Binding binding = site.Bindings.First();

                string id = site.Id.ToString();
                string idb = binding.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                newbindings = this.bindingTable.Query.Where(b => b.RowKey == idb);

                Assert.IsNotNull(newsite);

                this.repository.RemoveWebSite(site.Id);

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                Assert.IsNull(newsite);

                newbindings = this.bindingTable.Query.Where(b => b.RowKey == idb);

                Assert.IsNotNull(newbindings);
                Assert.AreEqual(0, newbindings.Count());
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void UpdateWebSite()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(1);
                Binding binding = site.Bindings.First();

                string id = site.Id.ToString();
                string idb = binding.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                newbindings = this.bindingTable.Query.Where(b => b.RowKey == idb);

                site.Name = "New Name";
                site.Description = "New Description";
                this.repository.UpdateWebSite(site);

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                Assert.IsNotNull(newsite);
                Assert.AreEqual(site.Name, newsite.Name);
                Assert.AreEqual(site.Description, newsite.Description);
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void UpdateBinding()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(1);
                Binding binding = site.Bindings.First();

                string id = site.Id.ToString();
                string idb = binding.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();
                newbindings = this.bindingTable.Query.Where(b => b.RowKey == idb);

                binding.HostName = "www.newhost.com";
                binding.IpAddress = "127.0.0.2";
                binding.Protocol = "https";
                binding.CertificateId = Guid.NewGuid();
                this.repository.UpdateBinding(binding);

                var newbinding = this.bindingTable.Query.Where(b => b.RowKey == idb).FirstOrDefault();
                Assert.IsNotNull(newbinding);
                Assert.AreEqual(binding.HostName, newbinding.HostName);
                Assert.AreEqual(binding.IpAddress, newbinding.IpAddress);
                Assert.AreEqual(binding.Protocol, newbinding.Protocol);
                Assert.AreEqual(binding.CertificateId, newbinding.CertificateId);
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void RemoveBinding()
        {
            WebSiteRow newsite = null;
            IEnumerable<BindingRow> newbindings = null;

            try
            {
                WebSite site = this.CreateWebSiteWithBindings(2);
                Binding binding = site.Bindings.First();

                string id = site.Id.ToString();
                string idb = binding.Id.ToString();

                newsite = this.webSiteTable.Query.Where(t => t.RowKey == id).FirstOrDefault();

                this.repository.RemoveBinding(binding.Id);

                newbindings = this.bindingTable.Query.Where(b => b.WebSiteId == site.Id);
                Assert.IsNotNull(newbindings);
                Assert.AreEqual(1, newbindings.Count());
            }
            finally
            {
                if (newsite != null)
                {
                    this.webSiteTable.DeleteEntity(newsite);
                }

                if (newbindings != null && newbindings.Count() > 0)
                {
                    this.bindingTable.DeleteEntity(newbindings);
                }
            }
        }

        [TestMethod]
        public void RetrieveWebSites()
        {
            IEnumerable<WebSiteRow> siteInfos = CreateAndSaveWebSiteRows(this.webSiteTable, 10);

            try
            {
                IEnumerable<WebSite> sites = this.repository.RetrieveWebSites();

                Assert.IsNotNull(sites);
                Assert.IsTrue(sites.Count() >= 10);

                foreach (var site in sites)
                {
                    Assert.AreNotEqual(Guid.Empty, site.Id);
                }

                foreach (WebSiteRow siteInfo in siteInfos)
                {
                    Assert.IsTrue(sites.Any(s => s.Id.ToString() == siteInfo.RowKey));
                }
            }
            finally
            {
                this.webSiteTable.DeleteEntity(siteInfos);
            }
        }

        private static IEnumerable<WebSiteRow> CreateAndSaveWebSiteRows(AzureTable<WebSiteRow> table, int count)
        {
            var sites = new List<WebSiteRow>();

            for (int k = 0; k < count; k++)
            {
                sites.Add(CreateWebSiteRow());
            }

            table.AddEntity(sites);

            return sites;
        }

        private static WebSiteRow CreateAndSaveWebSiteRow(AzureTable<WebSiteRow> table)
        {
            WebSiteRow site = CreateWebSiteRow();
            table.AddEntity(site);
            return site;
        }

        private static WebSiteRow CreateWebSiteRow()
        {
            var id = Guid.NewGuid();

            return new WebSiteRow(id)
            {
                Name = "Web Site " + id.ToString(),
                Description = "Description " + id.ToString()
            };
        }

        private WebSite CreateWebSiteWithBindings(int nbindings)
        {
            Guid id = Guid.NewGuid();
            var bindings = new List<Binding>();

            var site = new WebSite(id)
            {
                Name = "Test Web Site " + id.ToString(),
                Description = "Description Test Web Site " + id.ToString()
            };

            var binding = new Binding()
            {
                Protocol = "http",
                IpAddress = string.Empty,
                Port = 80,
                HostName = "www.test0.com"
            };

            this.repository.CreateWebSiteWithBinding(site, binding);
            bindings.Add(binding);

            for (int k = 1; k < nbindings; k++)
            {
                var otherBinding = new Binding()
                {
                    Protocol = "http",
                    IpAddress = string.Empty,
                    Port = 80 + k,
                    HostName = string.Format("www.test{0}.com", k)
                };

                this.repository.AddBindingToWebSite(site.Id, otherBinding);
                bindings.Add(otherBinding);
            }

            site.Bindings = bindings;

            return site;
        }
    }
}