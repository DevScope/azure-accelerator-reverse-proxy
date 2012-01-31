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
        private ISyncService syncService;

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

            Trace.TraceInformation("WebRole.OnStart");

            // Initialize local resources
            var localSitesPath = GetLocalResourcePathAndSetAccess("Sites");
            var localTempPath = GetLocalResourcePathAndSetAccess("TempSites");

            // Get settings
            var directoriesToExclude = RoleEnvironment.GetConfigurationSettingValue("DirectoriesToExclude").Split(';');

            // WebDeploy creates temporary files during package creation. The default TEMP location allows for a 100MB
            // quota (see http://msdn.microsoft.com/en-us/library/gg465400.aspx#Y976). 
            // For large web deploy packages, the synchronization process will raise an IO exception because the "disk is full" 
            // unless you ensure that the TEMP/TMP target directory has sufficient space
            Environment.SetEnvironmentVariable("TMP", localTempPath);
            Environment.SetEnvironmentVariable("TEMP", localTempPath);

            // Create the sync service and update the sites status
            this.syncService = new SyncService(localSitesPath, localTempPath, directoriesToExclude, "DataConnectionstring");
            this.syncService.Start();

            return base.OnStart();
        }

        public override void Run()
        {
            Trace.TraceInformation("WebRole.Run");
            var syncInterval = int.Parse(RoleEnvironment.GetConfigurationSettingValue("SyncIntervalInSeconds"), CultureInfo.InvariantCulture);
            this.syncService.SyncForever(TimeSpan.FromSeconds(syncInterval));
            while (true)
            {
                System.Threading.Thread.Sleep(10000);
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WebRole.OnStop");

            // Set the sites as not synced for this instance
            var roleInstanceId = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : Environment.MachineName;
            this.syncService.UpdateAllSitesSyncStatus(roleInstanceId, false);

            base.OnStop();
        }

        private static void ConfigureDiagnosticMonitor()
        {
            var transferPeriod = TimeSpan.FromMinutes(5);
            var bufferQuotaInMB = 100;

            // Add Windows Azure Trace Listener
            System.Diagnostics.Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());

            // Enable Collection of Crash Dumps
            CrashDumps.EnableCollection(true);

            // Get the Default Initial Config
            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Windows Azure Logs
            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.BufferQuotaInMB = bufferQuotaInMB;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Information;

            // File-based logs
            config.Directories.ScheduledTransferPeriod = transferPeriod;
            config.Directories.BufferQuotaInMB = bufferQuotaInMB;

            config.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = transferPeriod;
            config.DiagnosticInfrastructureLogs.BufferQuotaInMB = bufferQuotaInMB;
            config.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogLevel.Warning;

            // Windows Event logs
            config.WindowsEventLog.DataSources.Add("Application!*");
            config.WindowsEventLog.DataSources.Add("System!*");
            config.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;
            config.WindowsEventLog.ScheduledTransferLogLevelFilter = LogLevel.Information;
            config.WindowsEventLog.BufferQuotaInMB = bufferQuotaInMB;

            // Performance Counters
            var counters = new List<string> {
                @"\Processor(_Total)\% Processor Time",
                @"\Memory\Available MBytes",
                @"\ASP.NET Applications(__Total__)\Requests Total",
                @"\ASP.NET Applications(__Total__)\Requests/Sec",
                @"\ASP.NET\Requests Queued",
            };

            counters.ForEach(
                counter =>
                {
                    config.PerformanceCounters.DataSources.Add(
                        new PerformanceCounterConfiguration { CounterSpecifier = counter, SampleRate = TimeSpan.FromSeconds(60) });
                });
            config.PerformanceCounters.ScheduledTransferPeriod = transferPeriod;
            config.PerformanceCounters.BufferQuotaInMB = bufferQuotaInMB;

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);
        }

        private static string GetLocalResourcePathAndSetAccess(string localResourceName)
        {
            string resourcePath = RoleEnvironment.GetLocalResource(localResourceName).RootPath.TrimEnd('\\');

            var localDataSec = Directory.GetAccessControl(resourcePath);
            localDataSec.AddAccessRule(new FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(resourcePath, localDataSec);

            return resourcePath;
        }
    }
}