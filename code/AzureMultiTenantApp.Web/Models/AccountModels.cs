namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models
{
    using System.ComponentModel.DataAnnotations;

    public class LogOnModel
    {
        [Required]
        [Display(Name = "User Name", Description = "The username that was configured during the new project wizard. This value is stored in your ServiceConfiguration.cscfg file.")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Description = "The password that was configured during the new project wizard. This value is stored in your ServiceConfiguration.cscfg file.")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}