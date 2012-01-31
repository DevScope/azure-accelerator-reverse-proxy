namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Helpers;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Web.Administration;
    using Microsoft.Web.Deployment;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.StorageClient;

    public class SyncService : ISyncService
    {
        private const string BlobStopName = "stop";

        private readonly IWebSiteRepository sitesRepository;
        private readonly ICertificateRepository certificateRepository;
        private readonly ISyncStatusRepository syncStatusRepository;

        private readonly string localSitesPath;
        private readonly string localTempPath;
        private readonly IEnumerable<string> directoriesToExclude;

        private readonly CloudBlobContainer container;

        private readonly IDictionary<string, FileEntry> entries;
        private readonly Dictionary<string, DateTime> siteDeployTimes;

        private static string roleWebSiteName = RoleEnvironment.CurrentRoleInstance.Id + "_" + "Web";

        public SyncService(string localSitesPath, string localTempPath, IEnumerable<string> directoriesToExclude, string storageSettingName)
            : this(
                    new WebSiteRepository(storageSettingName),
                    new CertificateRepository(storageSettingName),
                    new SyncStatusRepository(storageSettingName),
                    CloudStorageAccount.FromConfigurationSetting(storageSettingName),
                    localSitesPath,
                    localTempPath,
                    directoriesToExclude)
        {
        }

        public SyncService(IWebSiteRepository sitesRepository, ICertificateRepository certificateRepository, ISyncStatusRepository syncStatusRepository, CloudStorageAccount storageAccount, string localSitesPath, string localTempPath, IEnumerable<string> directoriesToExclude)
        {
            this.sitesRepository = sitesRepository;
            this.certificateRepository = certificateRepository;
            this.syncStatusRepository = syncStatusRepository;

            this.localSitesPath = localSitesPath;
            this.localTempPath = localTempPath;
            this.directoriesToExclude = directoriesToExclude;
            this.entries = new Dictionary<string, FileEntry>();
            this.siteDeployTimes = new Dictionary<string, DateTime>();

            var sitesContainerName = RoleEnvironment.GetConfigurationSettingValue("SitesContainerName").ToLowerInvariant();
            this.container = storageAccount.CreateCloudBlobClient().GetContainerReference(sitesContainerName);
            this.container.CreateIfNotExist();
        }

        public void Start()
        {
            TraceHelper.TraceInformation("Sync service starting...");
            this.SyncOnce();
        }

        public void SyncForever(TimeSpan interval)
        {
            var blobStop = this.container.GetBlobReference(BlobStopName);
            var lastHeartbeat = DateTime.MinValue;

            while (true)
            {
                bool isPaused = blobStop.Exists();
 
                var currentTime = DateTime.Now;
                if ((currentTime - lastHeartbeat).Minutes > 15)
                {
                    TraceHelper.TraceInformation("SyncService - Synchronization is {0}...", isPaused ? "paused" : "active");
                    lastHeartbeat = currentTime;
                }

                if (!isPaused)
                {
                    SyncOnce();
                }

                Thread.Sleep(interval);
            }
        }

        private void SyncOnce()
        {
            TraceHelper.TraceVerbose("SyncService - Synchronizing role instances...");

            try
            {
                this.UpdateIISSitesFromTableStorage();
            }
            catch (Exception e)
            {
                TraceHelper.TraceError("SyncService [Table => IIS] - Failed to update IIS site information from table storage.{0}{1}", Environment.NewLine, e.TraceInformation());
            }

            try
            {
                this.SyncBlobToLocal();
            }
            catch (Exception e)
            {
                TraceHelper.TraceError("SyncService [Blob => Local Storage] - Failed to synchronize local site folders and blob storage.{0}{1}", Environment.NewLine, e.TraceInformation());
            }

            try
            {
                this.DeploySitesFromLocal();
            }
            catch (Exception e)
            {
                TraceHelper.TraceError("SyncService [Local Storage => IIS] - Failed to deploy MSDeploy package in local storage to IIS.{0}{1}", Environment.NewLine, e.TraceInformation());
            }

            try
            {
                this.PackageSitesToLocal();
            }
            catch (Exception e)
            {
                TraceHelper.TraceError("SyncService [IIS => Local Storage] - Failed to create an MSDeploy package in local storage from updates in IIS.{0}{1}", Environment.NewLine, e.TraceInformation());
            }

            TraceHelper.TraceVerbose("SyncService - Synchronization completed.");
        }

        public static bool IsSyncEnabled()
        {
            var blobStop = GetCloudBlobStop();
            var enable = !blobStop.Exists();
            return enable;
        }

        public static void SyncEnable()
        {
            var blobStop = GetCloudBlobStop();
            blobStop.DeleteIfExists();

            TraceHelper.TraceInformation("SyncService - Synchronization resumed.");
        }

        public static void SyncDisable()
        {
            var blobStop = GetCloudBlobStop();
            if (!blobStop.Exists())
            {
                blobStop.UploadText(string.Empty); 
            }

            TraceHelper.TraceInformation("SyncService - Synchronization paused.");
        }

        private static CloudBlob GetCloudBlobStop()
        {
            var storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionstring");
            var sitesContainerName = RoleEnvironment.GetConfigurationSettingValue("SitesContainerName").ToLowerInvariant();
            var container = storageAccount.CreateCloudBlobClient().GetContainerReference(sitesContainerName);
            var blobStop = container.GetBlobReference(BlobStopName);
            return blobStop;
        }

        private static DateTime GetFolderLastModifiedTimeUtc(string sitePath)
        {
            try
            {
                var lastModifiedTime = File.GetLastWriteTimeUtc(sitePath);

                foreach (var filePath in Directory.EnumerateFileSystemEntries(sitePath, "*", SearchOption.AllDirectories))
                {
                    var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
                    if (fileLastWriteTimeUtc > lastModifiedTime)
                    {
                        lastModifiedTime = fileLastWriteTimeUtc;
                    }
                }

                return lastModifiedTime;
            }
            catch (PathTooLongException e)
            {
                TraceHelper.TraceError("SyncService - Failed to retrieve last modified time.{0}{1}", Environment.NewLine, e.TraceInformation());

                return DateTime.MinValue;
            }
        }

        private void UpdateIISSitesFromTableStorage()
        {
            var allSites = this.sitesRepository.RetrieveWebSitesWithBindingsAndCertificates(this.certificateRepository);

            if (!WindowsAzureHelper.IsComputeEmulatorEnvironment)
            {
                var iisManager = new IISManager(this.localSitesPath, this.localTempPath, this.syncStatusRepository);
                iisManager.UpdateSites(allSites);
            }

            // Cleanup
            for (int i = this.siteDeployTimes.Count - 1; i >= 0; i--)
            {
                var siteName = this.siteDeployTimes.ElementAt(i).Key;
                if (!allSites.Any(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase)))
                {
                    this.siteDeployTimes.Remove(siteName);
                    this.syncStatusRepository.RemoveWebSiteStatus(siteName);

                    var sitePath = Path.Combine(this.localSitesPath, siteName);
                    var tempSitePath = Path.Combine(this.localTempPath, siteName);

                    FilesHelper.RemoveFolder(sitePath);
                    FilesHelper.RemoveFolder(tempSitePath);

                    if (this.entries.ContainsKey(siteName))
                    {
                        // Remove blob
                        this.container.GetBlobReference(siteName).DeleteIfExists();
                        this.container.GetBlobReference(siteName + "/" + siteName + ".zip").DeleteIfExists();
                        
                        this.entries.Remove(siteName);
                    }
                }
            }
        }

        private void SyncBlobToLocal()
        {
            var seen = new HashSet<string>();

            foreach (var thing in this.EnumerateLocalEntries())
            {
                var path = thing.Item1;
                var entry = thing.Item2;

                seen.Add(path);

                if (!this.entries.ContainsKey(path) || this.entries[path].LocalLastModified < entry.LocalLastModified)
                {
                    var newBlob = this.container.GetBlobReference(path);
                    if (entry.IsDirectory)
                    {
                        newBlob.Metadata["IsDirectory"] = bool.TrueString;
                        newBlob.UploadByteArray(new byte[0]);
                    }
                    else
                    {
                        using (var stream = File.Open(Path.Combine(this.localTempPath, path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        {
                            newBlob.Metadata["IsDirectory"] = bool.FalseString;
                            newBlob.UploadFromStream(stream);
                        }
                    }

                    entry.CloudLastModified = newBlob.Properties.LastModifiedUtc;
                    this.entries[path] = entry;
                }
            }

            foreach (var path in this.entries.Keys.Where(k => !seen.Contains(k)).ToArray())
            {
                // Try deleting all the unused files and directories
                try
                {
                    if (this.entries[path].IsDirectory)
                    {
                        Directory.Delete(path);
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                }

                this.entries.Remove(path);
            }

            seen = new HashSet<string>();

            var blobs = this.container.ListBlobs(
                new BlobRequestOptions 
                { 
                    UseFlatBlobListing = true, 
                    BlobListingDetails = BlobListingDetails.Metadata
                }).OfType<CloudBlob>();

            foreach (var blob in blobs)
            {
                var path = blob.Uri.ToString().Substring(this.container.Uri.ToString().Length + 1);
                var entry = new FileEntry
                {
                    CloudLastModified = blob.Properties.LastModifiedUtc,
                    IsDirectory = blob.Metadata.AllKeys.Any(k => k.Equals("IsDirectory")) && 
                                  blob.Metadata["IsDirectory"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
                };

                seen.Add(path);

                if (!this.entries.ContainsKey(path) || this.entries[path].CloudLastModified < entry.CloudLastModified)
                {
                    var tempPath = Path.Combine(this.localTempPath, path);
                    
                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(tempPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.Combine(this.localTempPath, Path.GetDirectoryName(path)));
                        TraceHelper.TraceInformation("SyncService [Blob => Local Storage] - Downloading file: '{0}'", path);

                        using (var stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                        {
                            blob.DownloadToStream(stream);
                        }
                    }

                    entry.LocalLastModified = new FileInfo(tempPath).LastWriteTimeUtc;
                    this.entries[path] = entry;
                }
            }

            foreach (var path in this.entries.Keys.Where(k => !seen.Contains(k)).ToArray())
            {
                if (this.entries[path].IsDirectory)
                {
                    Directory.Delete(Path.Combine(this.localTempPath, path), true);
                }
                else
                {
                    try
                    {
                        File.Delete(Path.Combine(this.localTempPath, path));
                    }
                    catch
                    {
                    }
                }

                this.entries.Remove(path);
            }
        }

        private void DeploySitesFromLocal()
        {
            TraceHelper.TraceVerbose("SyncService [Local Storage => IIS] - Site deploy times: {0}", string.Join(",", this.siteDeployTimes.Select(t => t.Key + " - " + t.Value).ToArray()));

            foreach (var site in Directory.EnumerateDirectories(this.localTempPath).Select(d => Path.GetFileName(d).ToLowerInvariant()))
            {
                var sitePath = Path.Combine(this.localSitesPath, site);
                var tempSitePath = Path.Combine(this.localTempPath, site);

                if (Directory.Exists(tempSitePath))
                {
                    // Sync from package to IIS App using MSDeploy
                    string packageFile = null;
                    try
                    {
                        packageFile = Directory.EnumerateFiles(tempSitePath).SingleOrDefault(f => f.ToLowerInvariant().EndsWith(".zip"));
                    }
                    catch (InvalidOperationException e)
                    {
                        if (string.IsNullOrEmpty(e.Message))
                        {
                            throw new InvalidOperationException("Multiple packages exist for the site '" + site + "'.");
                        }

                        throw;
                    }

                    if (packageFile != null)
                    {
                        if (!this.siteDeployTimes.ContainsKey(site))
                        {
                            this.siteDeployTimes.Add(site, DateTime.MinValue);
                        }

                        var packageLastModifiedTime = Directory.GetLastWriteTimeUtc(packageFile);
                        TraceHelper.TraceVerbose("SyncService [Local Storage => IIS] - Package last modified time: '{0}'", packageLastModifiedTime);

                        if (this.siteDeployTimes[site] < packageLastModifiedTime)
                        {
                            TraceHelper.TraceInformation("SyncService [Local Storage => IIS] - Deploying the package '{0}' to '{1}' with MSDeploy", packageFile, sitePath);

                            try
                            {
                                using (DeploymentObject deploymentObject = DeploymentManager.CreateObject(DeploymentWellKnownProvider.Package, packageFile))
                                {
                                    deploymentObject.SyncTo(DeploymentWellKnownProvider.DirPath, sitePath, new DeploymentBaseOptions(), new DeploymentSyncOptions());
                                }

                                this.UpdateSyncStatus(site, SyncInstanceStatus.Deployed);
                                this.siteDeployTimes[site] = DateTime.UtcNow;
                            }
                            catch (Exception)
                            {
                                this.UpdateSyncStatus(site, SyncInstanceStatus.Error);
                                throw;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Packages sites that are in IIS but not in local temp storage.
        /// There are new sites that have been deployed to this instance using Web Deploy.
        /// </summary>
        private void PackageSitesToLocal()
        {
            TraceHelper.TraceVerbose("SyncService [IIS => Local Storage] - Site deploy times: {0}", string.Join(",", this.siteDeployTimes.Select(t => t.Key + " - " + t.Value).ToArray()));

            using (var serverManager = new ServerManager())
            {
                foreach (var site in serverManager.Sites.ToArray())
                {
                    var siteName = site.Name.Replace("-", ".").ToLowerInvariant();
                    
                    if (!site.Name.Equals(roleWebSiteName, StringComparison.OrdinalIgnoreCase))
                    {                        
                        var sitePath = Path.Combine(this.localSitesPath, siteName);
                        var siteLastModifiedTime = GetFolderLastModifiedTimeUtc(sitePath);

                        if (!this.siteDeployTimes.ContainsKey(siteName))
                        {
                            this.siteDeployTimes.Add(siteName, siteLastModifiedTime);
                        }

                        TraceHelper.TraceVerbose("SyncService [IIS => Local Storage] - Site last modified time: '{0}'", siteLastModifiedTime);

                        if (this.siteDeployTimes[siteName] < siteLastModifiedTime && siteLastModifiedTime > DateTime.UtcNow.AddSeconds(30))
                        {
                            this.UpdateSyncStatus(siteName, SyncInstanceStatus.Deployed);

                            var tempSitePath = Path.Combine(this.localTempPath, siteName);
                            if (!Directory.Exists(tempSitePath))
                            {
                                Directory.CreateDirectory(tempSitePath);
                            }
                            
                            var packageFile = Path.Combine(tempSitePath, siteName + ".zip");

                            // Create a package of the site and move it to local temp sites
                            TraceHelper.TraceInformation("SyncService [IIS => Local Storage] - Creating a package of the site '{0}' and moving it to local temp sites '{1}'", siteName, packageFile);

                            try
                            {
                                using (DeploymentObject deploymentObject = DeploymentManager.CreateObject(DeploymentWellKnownProvider.DirPath, sitePath))
                                {
                                    deploymentObject.SyncTo(DeploymentWellKnownProvider.Package, packageFile, new DeploymentBaseOptions(), new DeploymentSyncOptions());
                                }

                                this.siteDeployTimes[siteName] = DateTime.UtcNow;
                            }
                            catch (Exception)
                            {
                                this.UpdateSyncStatus(siteName, SyncInstanceStatus.Error);
                                throw;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Tuple<string, FileEntry>> EnumerateLocalEntries()
        {
            foreach (var filePath in Directory.EnumerateFileSystemEntries(this.localTempPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = filePath.Substring(this.localTempPath.Length + 1).Replace('\\', '/');
                var info = new FileInfo(filePath);
                var entry = new FileEntry
                {
                    LocalLastModified = info.LastWriteTimeUtc,
                    IsDirectory = info.Attributes.HasFlag(FileAttributes.Directory)
                };

                if (this.IsExcluded(relativePath))
                {
                    continue;
                }

                yield return new Tuple<string, FileEntry>(relativePath, entry);
            }
        }

        private bool IsExcluded(string topPath)
        {
            int position = topPath.IndexOf('/');

            if (position <= 0)
            {
                return false;
            }

            // Remove Site name
            string path = topPath.Substring(position + 1);

            if (this.directoriesToExclude.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string toExclude in this.directoriesToExclude)
            {
                if (path.StartsWith(toExclude + "/"))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateSyncStatus(string webSiteName, SyncInstanceStatus status)
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