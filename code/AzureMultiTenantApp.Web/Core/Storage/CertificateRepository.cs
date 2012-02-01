namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;
    using System.Diagnostics;
    using System.Web.Helpers;
    using System.IO;

    public class CertificateRepository
    {

        public void PopulateRepository()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            var certificates = new List<Certificate>();
            foreach (var cert in store.Certificates)
            {
                if (!String.IsNullOrEmpty(cert.FriendlyName))
                {
                    certificates.Add(new Certificate
                    {
                        Thumbprint = cert.Thumbprint,
                    });
                }
            }
            store.Close();

            string appRoot = Environment.GetEnvironmentVariable("RoleRoot");
            var temp = Path.Combine(appRoot, "approot\\bin\\certs.json");
            var json = Json.Encode(certificates);
            File.WriteAllText(temp, json);
        }

        public List<Certificate> RetrieveCertificates()
        {
            string appRoot = Environment.GetEnvironmentVariable("RoleRoot");
            var temp = Path.Combine(appRoot, "approot\\bin\\certs.json");
            try
            {
                var json = File.ReadAllText(temp);
                return Json.Decode<List<Certificate>>(json);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Could not retrieve certificates. See next message for details.");
                Trace.TraceError(ex.TraceInformation());
                return new List<Certificate>();
            }
        }

    }
}