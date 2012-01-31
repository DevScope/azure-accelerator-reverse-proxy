namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

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