namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class LogMessageModel
    {
        [Display(Name = "LogTimestamp")]
        public DateTime LogTimestamp { get; set; }

        [Display(Name = "Level")]
        public string Level { get; set; }

        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "RoleInstanceId")]
        public string RoleInstanceId { get; set; }
    }

    public class SyncStatusModel
    {
        [Display(Name = "RoleInstanceId")]
        public string RoleInstanceId { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "LogTimestamp")]
        public DateTime SyncTimestamp { get; set; }
    }
}