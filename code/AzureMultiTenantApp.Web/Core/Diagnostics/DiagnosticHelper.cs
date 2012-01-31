using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics
{
    public class DiagnosticHelper
    {
        private static string wadConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        private static IEnumerable<string> ConfiguredCounters
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

        public static void ConfigureDiagnosticMonitor()
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

    }
}