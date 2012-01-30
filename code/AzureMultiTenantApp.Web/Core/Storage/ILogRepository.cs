namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System.Collections.Generic;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;

    public interface ILogRepository
    {
        IEnumerable<LogMessage> RetrieveLogMessages(int count);

        IEnumerable<LogMessage> RetrieveLogMessages(int count, string level);
    }
}