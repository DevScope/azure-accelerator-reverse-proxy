namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;

    public class LogRepository : ILogRepository
    {
        private readonly IAzureTable<AzureLogMessage> table;

        public LogRepository()
            : this("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString")
        {
        }

        public LogRepository(string settingName)
            : this(new AzureTable<AzureLogMessage>(CloudStorageAccount.FromConfigurationSetting(settingName), "WebHostTracingEntries"))
        {
        }

        public LogRepository(IAzureTable<AzureLogMessage> table)
        {
            this.table = table;
            this.table.CreateIfNotExist();
        }

        public IEnumerable<LogMessage> RetrieveLogMessages(int count)
        {
            return this.RetrieveLogMessages(count, string.Empty);
        }

        public IEnumerable<LogMessage> RetrieveLogMessages(int count, string level)
        {
            if (!string.IsNullOrWhiteSpace(level))
            {
                return this.table.Query.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).Take(count).ToList().Select(l => l.ToModel());
            }
            else
            {
                return this.table.Query.Take(count).ToList().Select(l => l.ToModel());
            }
        }
    }
}