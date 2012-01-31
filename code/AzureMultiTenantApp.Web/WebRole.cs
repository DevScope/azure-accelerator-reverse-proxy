namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.AccessControl;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.Diagnostics.Management;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        private static string wadConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        private ISyncStatusRepository syncStatusRepository;
        private ISyncService syncService;

        public static IEnumerable<string> ConfiguredCounters
        {
            get
            {
                yield return @"\Processor(_Total)\% Processor Time";
                yield return @"\Memory\Available MBytes";
                yield return @"\ASP.NET Applications(__Total__)\Requests Total";
                yield return @"\ASP.NET Applications(__Total__)\Requests/Sec";
                yield return @"\ASP.NET\Requests Queued";
            }
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                string configuration = RoleEnvironment.IsAvailable ?
                    RoleEnvironment.GetConfigurationSettingValue(configName) :
                    ConfigurationManager.AppSettings[configName];

                configSetter(configuration);
            });

            ConfigureDiagnosticMonitor();

            this.syncStatusRepository = new SyncStatusRepository();
            this.UpdateAllSitesSyncStatus(true);
            
            Trace.TraceInformation("WebRole.OnStart");

            return base.OnStart();
        }

        public override void Run()
        {
            try
            {
                Trace.TraceInformation("WebRole.Run");

                // Initialize SyncService
                var localSitesPath = GetLocalResourcePathAndSetAccess("Sites");
                var localTempPath = GetLocalResourcePathAndSetAccess("TempSites");
                var directoriesToExclude = RoleEnvironment.GetConfigurationSettingValue("DirectoriesToExclude").Split(';');
                var syncInterval = int.Parse(RoleEnvironment.GetConfigurationSettingValue("SyncIntervalInSeconds"), CultureInfo.InvariantCulture);

                // WebDeploy creates temporary files during package creation. The default TEMP location allows for a 100MB
                // quota (see http://msdn.microsoft.com/en-us/library/gg465400.aspx#Y976). 
                // For large web deploy packages, the synchronization process will raise an IO exception because the "disk is full" 
                // unless you ensure that the TEMP/TMP target directory has sufficient space
                Environment.SetEnvironmentVariable("TMP", localTempPath);
                Environment.SetEnvironmentVariable("TEMP", localTempPath);

                this.syncService = new SyncService(localSitesPath, localTempPath, directoriesToExclude, "DataConnectionstring");
                this.syncService.SyncForever(TimeSpan.FromSeconds(syncInterval));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.TraceInformation());
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WebRole.OnStop");

            this.UpdateAllSitesSyncStatus(false);

            base.OnStop();
        }

        private static void ConfigureDiagnosticMonitor()
        {
            var storageAccount = CloudStorageAccount.FromConfigurationSetting(wadConnectionString);
            var roleInstanceDiagnosticManager = storageAccount.CreateRoleInstanceDiagnosticManager(RoleEnvironment.DeploymentId, RoleEnvironment.CurrentRoleInstance.Role.Name, RoleEnvironment.CurrentRoleInstance.Id);
            var diagnosticMonitorConfiguration = roleInstanceDiagnosticManager.GetCurrentConfiguration();
            if (diagnosticMonitorConfiguration == null)
            {
                diagnosticMonitorConfiguration = DiagnosticMonitor.GetDefaultInitialConfiguration();
            }

            // File-based logs
            diagnosticMonitorConfiguration.Directories.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            diagnosticMonitorConfiguration.Directories.BufferQuotaInMB = 100;

            // Windows Event logs
            diagnosticMonitorConfiguration.WindowsEventLog.DataSources.Add("Application!*");
            diagnosticMonitorConfiguration.WindowsEventLog.DataSources.Add("System!*");
            diagnosticMonitorConfiguration.WindowsEventLog.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);

            // Performance Counters
            ConfiguredCounters.ToList().ForEach(
                counter =>
                {
                    var counterConfiguration = new PerformanceCounterConfiguration
                    { 
                        CounterSpecifier = counter,
                        SampleRate = TimeSpan.FromSeconds(30)
                    };

                    diagnosticMonitorConfiguration.PerformanceCounters.DataSources.Add(counterConfiguration);
                });
            
            diagnosticMonitorConfiguration.PerformanceCounters.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);

            roleInstanceDiagnosticManager.SetCurrentConfiguration(diagnosticMonitorConfiguration);
        }

        private static string GetLocalResourcePathAndSetAccess(string localResourceName)
        {
            string resourcePath = RoleEnvironment.GetLocalResource(localResourceName).RootPath.TrimEnd('\\');

            var localDataSec = Directory.GetAccessControl(resourcePath);
            localDataSec.AddAccessRule(new FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(resourcePath, localDataSec);

            return resourcePath;
        }

        private void UpdateAllSitesSyncStatus(bool isOnline)
        {
            if (this.syncStatusRepository == null)
            {
                return;
            }

            var roleInstanceId = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : Environment.MachineName;
            SyncStatus newSyncStatus;

            foreach (var syncStatus in this.syncStatusRepository.RetrieveSyncStatusByInstanceId(roleInstanceId))
            {
                newSyncStatus = new SyncStatus
                {
                    SiteName = syncStatus.SiteName,
                    RoleInstanceId = roleInstanceId,
                    Status = syncStatus.Status,
                    IsOnline = isOnline
                };

                this.syncStatusRepository.UpdateStatus(newSyncStatus);
            } 
        }
    }
}