namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;

    public class LogMessage
    {
        public DateTime LogTimestamp { get; set; }

        public string Level { get; set; }

        public string Message { get; set; }

        public string RoleInstanceId { get; set; }
    }
}