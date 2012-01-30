namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;

    public interface ICertificateRepository
    {
        void CreateCertificate(Certificate certificate);
        
        IEnumerable<Certificate> RetrieveCertificates();

        Certificate RetrieveCertificateWithBindings(Guid certificateId);

        Certificate RetrieveCertificateForBinding(Binding binding);

        void RemoveCertificate(Guid certificateId);

        void UpdateCertificate(Certificate certificate);

        X509Certificate2 InstallCertificate(Certificate certificate, X509Store store);
    }
}