namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Security.Permissions;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class AzureTableTraceListener : SimplifiedTraceListener
    {
        private const int LogFlushIntervalInMilliseconds = 1 * 1000;
        private readonly IAzureTable<AzureLogMessage> table;
        private readonly string roleInstanceId;

        public AzureTableTraceListener()
            : this("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")
        {
        }

        public AzureTableTraceListener(string settingName)
            : this(new AzureTable<AzureLogMessage>(CloudStorageAccount.FromConfigurationSetting(settingName), "WebHostTracingEntries"))
        {
        }

        public AzureTableTraceListener(IAzureTable<AzureLogMessage> table)
            : base("AzureTableTraceListener")
        {
            this.roleInstanceId = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : Environment.MachineName;
            
            this.table = table;
            this.table.CreateIfNotExist();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void TraceEventCore(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, string message)
        {
            var logMessage = new AzureLogMessage
            {
                ActivityId = Trace.CorrelationManager.ActivityId.ToString(),
                LogTimestamp = eventCache.DateTime,
                Level = eventType.ToString(),
                Message = message,
                RoleInstanceId = this.roleInstanceId
            };

            logMessage.PartitionKey = logMessage.ActivityId;
            logMessage.RowKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);

            this.table.AddEntity(logMessage);
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void TraceDataCore(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data)
        {
            var logMessage = new AzureLogMessage
            {
                ActivityId = Trace.CorrelationManager.ActivityId.ToString(),
                LogTimestamp = eventCache.DateTime,
                Level = eventType.ToString(),
                Message = string.Join(",", data),
                RoleInstanceId = this.roleInstanceId
            };

            logMessage.PartitionKey = logMessage.ActivityId;
            logMessage.RowKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);

            this.table.AddEntity(logMessage);
        }
    }
}