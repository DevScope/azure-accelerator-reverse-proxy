namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Tests.Controllers
{
    using System;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Controllers;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;

    [TestClass]
    public class WebSiteControllerTests
    {
        private WebSiteRepository webSiteRepository;
        private CertificateRepository certificateRepository;
        private AzureTable<WebSiteRow> webSiteTable;
        private AzureTable<BindingRow> bindingTable;
        private WebSiteController controller;

        [TestInitialize]
        public void Setup()
        {
            this.webSiteTable = new AzureTable<WebSiteRow>(CloudStorageAccount.DevelopmentStorageAccount, "WebSitesTest");
            this.bindingTable = new AzureTable<BindingRow>(CloudStorageAccount.DevelopmentStorageAccount, "BindingsTest");
            this.certificateRepository = new CertificateRepository();
            this.webSiteTable.CreateIfNotExist();
            this.bindingTable.CreateIfNotExist();
            this.webSiteRepository = new WebSiteRepository(this.webSiteTable, this.bindingTable);
            this.controller = new WebSiteController(this.webSiteRepository, this.certificateRepository);
        }

        [TestMethod]
        public void CreateWebSiteWithBindings()
        {
            Guid id = Guid.NewGuid();

            var model = new WebSiteCreateModel()
            {
                Name = "testsite" + id.ToString().ToLowerInvariant(),
                Description = "Test Description",
                Port = 80,
                IpAddress = string.Empty,
                HostName = "www.mydomain.com",
                Protocol = "http"
            };

            this.controller.Create(model);

            WebSite newsite = this.webSiteRepository.RetrieveWebSites().Where(ws => ws.Name == model.Name).FirstOrDefault();

            try
            {
                Assert.IsNotNull(newsite);
                Assert.AreEqual(model.Name, newsite.Name);
                Assert.AreEqual(model.Description, newsite.Description);

                WebSite site = this.webSiteRepository.RetrieveWebSiteWithBindings(newsite.Id);

                Assert.IsNotNull(site);
                Assert.IsNotNull(site.Bindings);
                Assert.AreEqual(1, site.Bindings.Count());

                Binding binding = site.Bindings.First();

                Assert.AreEqual(model.Port, binding.Port);
                Assert.AreEqual(site.Id, binding.WebSiteId);
                Assert.AreEqual(model.IpAddress, binding.IpAddress);
                Assert.AreEqual(model.HostName, binding.HostName);
                Assert.AreEqual(model.Protocol, binding.Protocol);
            }
            finally
            {
                string key = newsite.Id.ToString();
                this.bindingTable.DeleteEntity(this.bindingTable.Query.Where(b => b.WebSiteId == newsite.Id));
                this.webSiteTable.DeleteEntity(this.webSiteTable.Query.Where(b => b.RowKey == key));
            }
        }
    }
}