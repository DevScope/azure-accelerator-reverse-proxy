namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Tests.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;

    [TestClass]
    public class CertificateRepositoryTests
    {
        private CertificateRepository certificateRepository;
        private WebSiteRepository webSiteRepository;
        private AzureTable<CertificateRow> certificateTable;
        private AzureTable<WebSiteRow> webSiteTable;
        private AzureTable<BindingRow> bindingTable;
        private AzureBlobContainer<byte[]> certificateBlobContainer;

        [TestInitialize]
        public void Setup()
        {
            this.webSiteTable = new AzureTable<WebSiteRow>(CloudStorageAccount.DevelopmentStorageAccount, "WebSitesTest");
            this.bindingTable = new AzureTable<BindingRow>(CloudStorageAccount.DevelopmentStorageAccount, "BindingsTest");
            this.webSiteTable.CreateIfNotExist();
            this.bindingTable.CreateIfNotExist();
            this.webSiteRepository = new WebSiteRepository(this.webSiteTable, this.bindingTable);

            this.certificateTable = new AzureTable<CertificateRow>(CloudStorageAccount.DevelopmentStorageAccount, "CertificatesTest");
            this.certificateTable.CreateIfNotExist();
            this.certificateBlobContainer = new AzureBlobContainer<byte[]>(CloudStorageAccount.DevelopmentStorageAccount, "CertificatesTest");
            this.certificateBlobContainer.EnsureExist();
            this.certificateRepository = new CertificateRepository(this.certificateTable, this.certificateBlobContainer, this.webSiteRepository);
        }

        [TestMethod]
        public void CreateNewCertificate()
        {
            var certificate = new Certificate()
            {
                Name = "test.cert.1",
                Description = "Test Certificate 1",
                Password = "nobodyknows"
            };

            CertificateRow newCertificate = null;

            try
            {
                this.certificateRepository.CreateCertificate(certificate);

                string key = certificate.Id.ToString();

                newCertificate = this.certificateTable.Query.Where(c => c.RowKey == key).FirstOrDefault();

                Assert.IsNotNull(newCertificate);
            }
            finally
            {
                if (newCertificate != null)
                {
                    this.certificateTable.DeleteEntity(newCertificate);
                }
            }
        }

        [TestMethod]
        [DeploymentItem("Resources\\azureacceleratorstest.cloudapp.net.pfx")]
        public void CreateNewCertificateWithContent()
        {
            var certificate = new Certificate()
            {
                Name = "test.cert.1",
                Description = "Test Certificate 1",
                Password = "Passw0rd!",
                Content = File.ReadAllBytes("azureacceleratorstest.cloudapp.net.pfx")
            };

            CertificateRow newcertificate = null;

            try
            {
                this.certificateRepository.CreateCertificate(certificate);

                string key = certificate.Id.ToString();

                newcertificate = this.certificateTable.Query.Where(c => c.RowKey == key).FirstOrDefault();

                Assert.IsNotNull(newcertificate);

                Stream stream = this.certificateBlobContainer.GetFile(key);

                Assert.IsNotNull(stream);
                stream.Close();
            }
            finally
            {
                if (newcertificate != null)
                {
                    this.certificateTable.DeleteEntity(newcertificate);
                }

                this.certificateBlobContainer.Delete(certificate.Id.ToString());
            }
        }

        [TestMethod]
        public void RetrieveCertificates()
        {
            IEnumerable<CertificateRow> certificateRows = CreateAndSaveCertificateRows(this.certificateTable, 10);

            try
            {
                IEnumerable<Certificate> certificates = this.certificateRepository.RetrieveCertificates();

                Assert.IsNotNull(certificates);
                Assert.IsTrue(certificates.Count() >= 10);

                foreach (var certificate in certificates)
                {
                    Assert.AreNotEqual(Guid.Empty, certificate.Id);
                }

                foreach (CertificateRow certificateRow in certificateRows)
                {
                    Assert.IsTrue(certificates.Any(c => c.Id.ToString() == certificateRow.RowKey));
                }
            }
            finally
            {
                this.certificateTable.DeleteEntity(certificateRows);
            }
        }

        [TestMethod]
        public void RetrieveCertificate()
        {
            CertificateRow certificateRow = CreateAndSaveCertificateRow(this.certificateTable);

            try
            {
                Certificate certificate = this.certificateRepository.RetrieveCertificateWithBindings(new Guid(certificateRow.RowKey));

                Assert.IsNotNull(certificate);

                Assert.AreEqual(certificateRow.Name, certificate.Name);
                Assert.AreEqual(certificateRow.Description, certificate.Description);
                Assert.AreEqual(certificateRow.Password, certificate.Password);
            }
            finally
            {
                this.certificateTable.DeleteEntity(certificateRow);
            }
        }

        [TestMethod]
        [DeploymentItem("Resources\\azureacceleratorstest.cloudapp.net.pfx")]
        public void RetrieveCertificateWithBindings()
        {
            CertificateRow certificateRow = CreateAndSaveCertificateRow(this.certificateTable);
            Guid id = new Guid(certificateRow.RowKey);

            this.certificateBlobContainer.SaveFile(id.ToString(), File.ReadAllBytes("azureacceleratorstest.cloudapp.net.pfx"), CertificateRepository.CertificateContentType);

            WebSite webSite1 = null;
            WebSite webSite2 = null;

            try
            {
                webSite1 = this.CreateWebSiteWithBindings(1);
                webSite2 = this.CreateWebSiteWithBindings(1);

                webSite1.Bindings.First().CertificateId = id;
                webSite2.Bindings.First().CertificateId = id;

                this.webSiteRepository.UpdateBinding(webSite1.Bindings.First());
                this.webSiteRepository.UpdateBinding(webSite2.Bindings.First());

                Certificate certificate = this.certificateRepository.RetrieveCertificateWithBindings(new Guid(certificateRow.RowKey));

                Assert.IsNotNull(certificate);

                Assert.AreEqual(certificateRow.Name, certificate.Name);
                Assert.AreEqual(certificateRow.Description, certificate.Description);
                Assert.AreEqual(certificateRow.Password, certificate.Password);

                Assert.IsNotNull(certificate.Bindings);
                Assert.AreEqual(2, certificate.Bindings.Count());

                foreach (var binding in certificate.Bindings)
                {
                    Assert.IsNotNull(binding.WebSite);
                    Assert.IsTrue(binding.WebSite.Id == webSite1.Id || binding.WebSite.Id == webSite2.Id);
                }

                var sites = this.webSiteRepository.RetrieveWebSitesWithBindingsAndCertificates(this.certificateRepository).Where(ws => ws.Id == webSite1.Id || ws.Id == webSite2.Id);

                foreach (var site in sites)
                {
                    Assert.IsNotNull(site.Bindings);
                    Assert.AreEqual(1, site.Bindings.Count());
                    Assert.AreEqual(certificate.Id, site.Bindings.First().CertificateId.Value);
                    Assert.IsNotNull(site.Bindings.First().Certificate);
                    Assert.AreEqual(certificate.Id, site.Bindings.First().Certificate.Id);
                }

                var site1 = this.webSiteRepository.RetrieveWebSiteWithBindingsAndCertificates(sites.First().Id, this.certificateRepository);

                Assert.IsNotNull(site1);
                Assert.IsNotNull(site1.Bindings);
                Assert.IsNotNull(site1.Bindings.First().Certificate);
                Assert.IsNotNull(site1.Bindings.First().Certificate.Content);
            }
            finally
            {
                if (webSite1 != null)
                {
                    this.webSiteRepository.RemoveWebSite(webSite1.Id);
                }

                if (webSite2 != null)
                {
                    this.webSiteRepository.RemoveWebSite(webSite2.Id);
                }

                this.certificateTable.DeleteEntity(certificateRow);
            }
        }

        [TestMethod]
        public void RemoveCertificate()
        {
            CertificateRow info = CreateAndSaveCertificateRow(this.certificateTable);

            try
            {
                Guid id = new Guid(info.RowKey);
                Certificate certificate = this.certificateRepository.RetrieveCertificateWithBindings(id);

                Assert.IsNotNull(certificate);

                this.certificateRepository.RemoveCertificate(new Guid(info.RowKey));

                certificate = this.certificateRepository.RetrieveCertificateWithBindings(id);

                Assert.IsNull(certificate);
            }
            finally
            {
                if (info != null)
                {
                    this.certificateTable.DeleteEntity(info);
                }
            }
        }

        private static IEnumerable<CertificateRow> CreateAndSaveCertificateRows(AzureTable<CertificateRow> table, int count)
        {
            List<CertificateRow> infos = new List<CertificateRow>();

            for (int k = 0; k < count; k++)
            {
                infos.Add(CreateCertificateRow());
            }

            table.AddEntity(infos);

            return infos;
        }

        private static CertificateRow CreateAndSaveCertificateRow(AzureTable<CertificateRow> table)
        {
            CertificateRow info = CreateCertificateRow();
            table.AddEntity(info);
            return info;
        }

        private static CertificateRow CreateCertificateRow()
        {
            Guid id = Guid.NewGuid();

            return new CertificateRow(id)
            {
                Name = "Certificate " + id.ToString(),
                Description = "Description " + id.ToString(),
                Password = "Password " + id.ToString()
            };
        }

        private WebSite CreateWebSiteWithBindings(int nbindings)
        {
            Guid id = Guid.NewGuid();
            var bindings = new List<Binding>();

            WebSite site = new WebSite(id)
            {
                Name = "testwebsite" + id.ToString().ToLowerInvariant(),
                Description = "Description Test Web Site " + id.ToString()
            };

            Binding binding = new Binding()
            {
                Protocol = "http",
                IpAddress = string.Empty,
                Port = 80,
                HostName = "www.test0.com"
            };

            this.webSiteRepository.CreateWebSiteWithBinding(site, binding);
            bindings.Add(binding);

            for (int k = 1; k < nbindings; k++)
            {
                var otherbinding = new Binding()
                {
                    Protocol = "http",
                    IpAddress = string.Empty,
                    Port = 80 + k,
                    HostName = string.Format("www.test{0}.com", k)
                };

                this.webSiteRepository.AddBindingToWebSite(site.Id, binding);
                bindings.Add(otherbinding);
            }

            site.Bindings = bindings;

            return site;
        }
    }
}