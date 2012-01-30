namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;

    public class CertificateRepository : ICertificateRepository
    {
        public const string CertificateContentType = "application/x-x509-ca-certificate";

        private readonly AzureTable<CertificateRow> certificateTable;
        private readonly AzureBlobContainer<byte[]> certificateBlobContainer;
        private readonly IWebSiteRepository webSiteRepository;

        public CertificateRepository()
            : this("DataConnectionString")
        {
        }

        public CertificateRepository(string settingName)
            : this(CloudStorageAccount.FromConfigurationSetting(settingName), "BindingCertificates", "BindingCertificates")
        {
        }

        public CertificateRepository(CloudStorageAccount account, string certificateTableName, string certificateContainerName)
            : this(new AzureTable<CertificateRow>(account, certificateTableName), new AzureBlobContainer<byte[]>(account, certificateContainerName), new WebSiteRepository(account))
        {
        }

        public CertificateRepository(AzureTable<CertificateRow> certificateTable, AzureBlobContainer<byte[]> certificateBlobContainer, IWebSiteRepository webSiteRepository)
        {
            this.certificateTable = certificateTable;
            this.certificateBlobContainer = certificateBlobContainer;

            this.certificateTable.CreateIfNotExist();
            this.certificateBlobContainer.EnsureExist();

            this.webSiteRepository = webSiteRepository;
        }

        public void CreateCertificate(Certificate certificate)
        {
            this.certificateTable.AddEntity(certificate.ToRow());

            if (certificate.Content != null)
            {
                this.certificateBlobContainer.SaveFile(certificate.Id.ToString(), certificate.Content, CertificateContentType);
            }
        }

        public IEnumerable<Certificate> RetrieveCertificates()
        {
            return this.certificateTable.Query.ToList().OrderBy(t => t.Name).Select(c => c.ToModel()).ToList();
        }

        public Certificate RetrieveCertificateWithBindings(Guid certificateId)
        {
            string key = certificateId.ToString();

            Certificate certificate = this.certificateTable.Query.Where(c => c.RowKey == key).FirstOrDefault().ToModel();

            if (certificate == null)
            {
                return null;
            }

            certificate.Bindings = this.webSiteRepository.RetrieveCertificateBindings(certificate.Id);

            return certificate;
        }

        public Certificate RetrieveCertificateForBinding(Binding binding)
        {
            if (!binding.CertificateId.HasValue)
            {
                return null;
            }

            string key = binding.CertificateId.Value.ToString();

            Certificate certificate = this.certificateTable.Query.Where(c => c.RowKey == key).FirstOrDefault().ToModel();

            if (certificate != null)
            {
                certificate.Content = this.certificateBlobContainer.GetBytes(certificate.Id.ToString());
            }
            
            binding.Certificate = certificate;

            return certificate;
        }

        public void RemoveCertificate(Guid certificateId)
        {
            var bindings = this.webSiteRepository.RetrieveCertificateBindings(certificateId);

            foreach (var binding in bindings)
            {
                binding.CertificateId = null;
                this.webSiteRepository.UpdateBinding(binding);
            }

            string key = certificateId.ToString();

            var certificates = this.certificateTable.Query.Where(c => c.RowKey == key);
            this.certificateTable.DeleteEntity(certificates);
            this.certificateBlobContainer.Delete(key);
        }

        public void UpdateCertificate(Certificate certificate)
        {
            this.certificateTable.AddOrUpdateEntity(certificate.ToRow());
        }

        public X509Certificate2 InstallCertificate(Certificate certificate, X509Store store)
        {
            byte[] content = this.certificateBlobContainer.GetBytes(certificate.Id.ToString());
            var cert = new X509Certificate2(content, certificate.Password);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            
            TraceHelper.TraceInformation("Certificate {0} installed; Friendly Name: {1}", certificate.Name, cert.FriendlyName);

            return cert;
        }
    }
}