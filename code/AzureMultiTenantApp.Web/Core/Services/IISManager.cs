﻿namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Helpers;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Web.Administration;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class IISManager
    {
        private readonly SyncStatusRepository syncStatusRepository;
        private readonly string localSitesPath;
        private readonly string tempSitesPath;

        private static string roleWebSiteName = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id + "_" + "Web" : "Default Web Site";

        public IISManager(string localSitesPath, string tempSitesPath, SyncStatusRepository syncStatusRepository)
        {
            this.syncStatusRepository = syncStatusRepository;
            this.localSitesPath = localSitesPath;
            this.tempSitesPath = tempSitesPath;
        }

        public void UpdateSites(IEnumerable<WebSite> sites)
        {
            Trace.TraceInformation("IISManager.Sites list from table: {0}", string.Join(",", sites.Select(s => s.Name)));

            using (var serverManager = new ServerManager())
            {
                var iisSites = serverManager.Sites;

                Trace.TraceInformation("IISManager.Sites list from IIS: {0}", string.Join(",", iisSites.Select(s => s.Name)));

                // Find sites that need to be removed
                foreach (var iisSite in iisSites.ToArray())
                {
                    var name = iisSite.Name.ToLowerInvariant();

                    // Never delete "webRoleSiteName", which is the website for this web role
                    if (!name.Equals(roleWebSiteName, StringComparison.OrdinalIgnoreCase) &&
                        !sites.Select(s => s.Name.ToLowerInvariant()).Contains(name))
                    {
                        // Remove site
                        Trace.TraceInformation("IISManager.Removing site '{0}'", iisSite.Name);

                        serverManager.Sites.Remove(iisSite);

                        // Remove TEST and CDN applications
                        RemoveApplications(iisSites, name);

                        // Remove site path
                        try
                        {
                            var sitePath = Path.Combine(this.localSitesPath, iisSite.Name);
                            var tempSitePath = Path.Combine(this.tempSitesPath, iisSite.Name);

                            FilesHelper.RemoveFolder(sitePath);
                            FilesHelper.RemoveFolder(tempSitePath);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceWarning("IISManager.Remove Site Path Error{0}{1}", Environment.NewLine, e.TraceInformation());
                        }

                        // Remove appPool
                        var appPool = serverManager.ApplicationPools.SingleOrDefault(ap => ap.Name.Equals(iisSite.Name, StringComparison.OrdinalIgnoreCase));
                        if (appPool != null)
                        {
                            Trace.TraceInformation("IISManager.Removing appPool '{0}'", appPool.Name);

                            serverManager.ApplicationPools.Remove(appPool);
                        }
                    }
                }

                try
                {
                    serverManager.CommitChanges();
                }
                catch (Exception e)
                {
                    Trace.TraceError("IISManager.CommitChanges (Cleanup IIS){0}{1}", Environment.NewLine, e.TraceInformation());
                }
            }

            foreach (var site in sites)
            {
                using (var serverManager = new ServerManager())
                {
                    var siteName = site.Name.ToLowerInvariant().Replace(" ", string.Empty);
                    var iisSite = serverManager.Sites.SingleOrDefault(ap => ap.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                    var sitePath = Path.Combine(this.localSitesPath, siteName);

                    // Add new sites
                    if (iisSite == null)
                    {
                        // Update Status
                        this.UpdateSyncStatus(siteName, SyncInstanceStatus.NotCreated);

                        // Create physical path
                        if (!Directory.Exists(sitePath))
                        {
                            Directory.CreateDirectory(sitePath);
                        }

                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Resources.LandingPage.html"))
                        {
                            var fileContent = new StreamReader(stream).ReadToEnd().Replace("{WebSiteName}", siteName);
                            File.WriteAllText(Path.Combine(sitePath, "index.html"), fileContent);
                        }

                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Resources.LandingStyle.css"))
                        {
                            var fileContent = new StreamReader(stream).ReadToEnd();
                            File.WriteAllText(Path.Combine(sitePath, "Site.css"), fileContent);
                        }

                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Resources.PublishImage.png"))
                        {
                            var bitmap = new Bitmap(stream);
                            bitmap.Save(Path.Combine(sitePath, "publish.png"));
                        }

                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Resources.SolutionImage.png"))
                        {
                            var bitmap = new Bitmap(stream);
                            bitmap.Save(Path.Combine(sitePath, "solution.png"));
                        }

                        // Add web site
                        Trace.TraceInformation("IISManager.Adding site '{0}'", siteName);

                        var defaultBinding = site.Bindings.First();
                        var bindingInformation = GetBindingInformation(defaultBinding.IpAddress, defaultBinding.Port, defaultBinding.HostName);

                        X509Certificate2 cert = null;

                        if (!String.IsNullOrEmpty(defaultBinding.CertificateThumbprint))
                        {
                            cert = GetCertificate(defaultBinding.CertificateThumbprint);
                        }

                        if (cert != null)
                        {
                            Trace.TraceInformation("IISManager.Adding WebSite '{0}' with Binding Information '{1}' and Certificate '{2}'", site.Name, bindingInformation, cert.Thumbprint);

                            iisSite = serverManager.Sites.Add(
                                siteName,
                                bindingInformation,
                                sitePath,
                                cert.GetCertHash());
                        }
                        else
                        {
                            Trace.TraceInformation("IISManager.Adding WebSite '{0}' with Binding Information '{1}'", site.Name, bindingInformation);

                            iisSite = serverManager.Sites.Add(
                                siteName,
                                defaultBinding.Protocol,
                                bindingInformation,
                                sitePath);
                        }

                        // Create a new AppPool
                        var appPool = serverManager.ApplicationPools.SingleOrDefault(ap => ap.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                        if (appPool == null)
                        {
                            Trace.TraceInformation("IISManager.Adding AppPool '{0}' for site '{0}'", siteName);

                            appPool = serverManager.ApplicationPools.Add(siteName);
                            appPool.ManagedRuntimeVersion = "v4.0";
                            appPool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
                        }

                        iisSite.ApplicationDefaults.ApplicationPoolName = appPool.Name;

                        // Update TEST and CDN applications
                        UpdateApplications(site, serverManager, siteName, sitePath, appPool);

                        // Update Sync Status
                        this.UpdateSyncStatus(siteName, SyncInstanceStatus.Created);
                    }
                    else
                    {
                        // Update TEST and CDN applications
                        var appPool = serverManager.ApplicationPools.SingleOrDefault(ap => ap.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                        UpdateApplications(site, serverManager, siteName, sitePath, appPool);
                    }

                    // Find bindings that need to be removed
                    foreach (var binding in iisSite.Bindings.ToArray())
                    {
                        if (!site.Bindings.Any(b => AreEqualsBindings(binding, b)))
                        {
                            Trace.TraceInformation("IISManager.Removing binding with protocol: '{0}'", binding.Protocol);
                            iisSite.Bindings.Remove(binding);
                        }
                    }

                    // Add new bindings
                    foreach (var binding in site.Bindings)
                    {
                        var iisBinding = iisSite.Bindings.SingleOrDefault(b => AreEqualsBindings(b, binding));
                        if (iisBinding == null)
                        {
                            var bindingInformation = GetBindingInformation(binding.IpAddress, binding.Port, binding.HostName);

                            X509Certificate2 cert = null;

                            if (!String.IsNullOrEmpty(binding.CertificateThumbprint))
                            {
                                cert = GetCertificate(binding.CertificateThumbprint);
                            }

                            if (cert != null)
                            {
                                Trace.TraceInformation("IISManager.Adding Binding '{0}' for WebSite '{1}' with Binding Information '{2}' and Certificate '{3}'", binding.Id, site.Name, bindingInformation, cert.Thumbprint);
                                iisSite.Bindings.Add(bindingInformation, cert.GetCertHash(), StoreName.My.ToString());
                            }
                            else
                            {
                                Trace.TraceInformation("IISManager.Adding Binding '{0}' for WebSite '{1}' with Binding Information '{2}'", binding.Id, site.Name, bindingInformation);
                                iisSite.Bindings.Add(bindingInformation, binding.Protocol);
                            }
                        }
                    }

                    try
                    {
                        Trace.TraceInformation("IISManager.Committing Changes for site '{0}'", site.Name);
                        serverManager.CommitChanges();
                    }
                    catch (Exception e)
                    {
                        this.UpdateSyncStatus(siteName, SyncInstanceStatus.Error);
                        Trace.TraceError("IISManager.CommitChanges for site '{0}'{1}{2}", site.Name, Environment.NewLine, e.TraceInformation());
                    }
                }
            }
        }

        private X509Certificate2 GetCertificate(string certificateHash)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certificateHash, true);
            store.Close();

            X509Certificate2 cert = null;
            if (certs.Count == 1)
            {
                cert = certs[0];
            }

            return cert;
        }

        private static void UpdateApplications(WebSite site, ServerManager serverManager, string siteName, string sitePath, ApplicationPool appPool)
        {
            var iisSites = serverManager.Sites;
            var adminSite = iisSites[roleWebSiteName];

            var testApplication = adminSite.Applications.FirstOrDefault(
                app => app.Path.EndsWith("/test/" + siteName, StringComparison.OrdinalIgnoreCase));
            var cdnApplication = adminSite.Applications.FirstOrDefault(
                app => app.Path.EndsWith("/cdn/" + siteName, StringComparison.OrdinalIgnoreCase));

            if (site.EnableTestChildApplication)
            {
                if (testApplication == null)
                {
                    Trace.TraceInformation("IISManager.Adding Test application for site '{0}'", siteName);
                    testApplication = adminSite.Applications.Add("/test/" + siteName, sitePath);
                    testApplication.ApplicationPoolName = appPool.Name;
                }
            }
            else
            {
                if (testApplication != null)
                {
                    Trace.TraceInformation("IISManager.Removing Test application for site '{0}'", siteName);
                    adminSite.Applications.Remove(testApplication);
                }
            }

            if (site.EnableCDNChildApplication)
            {
                if (cdnApplication == null)
                {
                    Trace.TraceInformation("IISManager.Adding CDN application for site '{0}'", siteName);
                    cdnApplication = adminSite.Applications.Add("/cdn/" + siteName, Path.Combine(sitePath, "cdn"));
                    cdnApplication.ApplicationPoolName = appPool.Name;
                }
            }
            else
            {
                if (cdnApplication != null)
                {
                    Trace.TraceInformation("IISManager.Removing CDN application for site '{0}'", siteName);
                    adminSite.Applications.Remove(cdnApplication);
                }
            }
        }

        private static string GetBindingInformation(string address, int port, string hostName)
        {
            return address + ":" + port.ToString(CultureInfo.InvariantCulture) + ":" + hostName;
        }

        private static bool AreEqualsBindings(Microsoft.Web.Administration.Binding iisBinding, Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities.Binding binding)
        {
            var bindingAdress = binding.IpAddress == "*" ? "0.0.0.0" : binding.IpAddress;

            return
                iisBinding.Protocol.Equals(binding.Protocol, StringComparison.OrdinalIgnoreCase) &&
                iisBinding.EndPoint.Address.ToString().Equals(bindingAdress, StringComparison.OrdinalIgnoreCase) &&
                iisBinding.EndPoint.Port == binding.Port &&
                iisBinding.Host.Equals(binding.HostName, StringComparison.OrdinalIgnoreCase);
        }

        private static void RemoveApplications(SiteCollection iisSites, string siteName)
        {
            var adminSite = iisSites[roleWebSiteName];

            var applicationsToRemove = from app in adminSite.Applications
                                       where app.Path.EndsWith("/test/" + siteName, StringComparison.OrdinalIgnoreCase) ||
                                       app.Path.EndsWith("/cdn/" + siteName, StringComparison.OrdinalIgnoreCase)
                                       select app;

            Trace.TraceInformation("IISManager.Removing Test and CDN applications for site '{0}'", siteName);

            foreach (var app in applicationsToRemove.ToArray())
            {
                adminSite.Applications.Remove(app);
            }
        }

        private void UpdateSyncStatus(string webSiteName, SyncInstanceStatus status)
        {
            if (this.syncStatusRepository != null)
            {
                var syncStatus = new SyncStatus
                {
                    SiteName = webSiteName,
                    RoleInstanceId = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : Environment.MachineName,
                    Status = status,
                    IsOnline = true
                };

                this.syncStatusRepository.UpdateStatus(syncStatus);
            }
        }
    }
}