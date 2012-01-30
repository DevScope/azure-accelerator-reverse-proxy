namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public class WebSiteModel
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "IIS Site Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        public string Url { get; set; }

        public string TestUrl { get; set; }

        [Required]
        [Display(Description = "The test site will point to the same physical folder of the website you are setting up.")]
        public bool EnableTestChildApplication { get; set; }

        [Required]
        [Display(Description = "Maps the contents of the CDN folder of your site to the administration site's CDN folder to enable publishing through the Content Delivery Network (CDN).")]
        public bool EnableCDNChildApplication { get; set; }

        public IEnumerable<BindingModel> Bindings { get; set; }
    }

    public class WebSiteCreateModel
    {
        // Web Site
        [Required]
        [Display(Name = "IIS Site Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        // Initial Binding
        [Required]
        [Display(Name = "Protocol", Description = "Specify the protocol for the site binding.")]
        public string Protocol { get; set; }

        [Required]
        [Display(Name = "Port", Description = "Type the port on which HTTP.sys must listen for requests made to this site. If you select HTTP protocol, the default port is 80; if you select HTTPS protocol, the default port is 443. If you specify a port different from the default ports, clients must specify the port number in requests to the server or they will not connect to the site.")]
        public int Port { get; set; }

        [Required]
        [Display(Name = "IP Address (* = All Unassigned)", Description = "Type an IP address that users can use to access this site. If you select All Unassigned (*), this site will respond to requests for all IP addresses on the port and optional host name that you specify for this site, unless another site on the server has a binding on the same port but with a specific IP address.")]
        public string IpAddress { get; set; }

        [Required]
        [Display(Name = "Host Name", Description = "Type a host name if you want to assign one or more host names, also known as domain names, to one computer that uses a single IP address. If you specify a host name, clients must use the host name instead of the IP address to access the site.")]
        public string HostName { get; set; }

        [Display(Name = "SSL certificate", Description = "Select the certificate that you want the site to use for SSL.")]
        public Guid? CertificateId { get; set; }

        [Required]
        [Display(Description = "The test site will point to the same physical folder of the website you are setting up.")]
        public bool EnableTestChildApplication { get; set; }

        [Required]
        [Display(Description = "Maps the contents of the CDN folder of your site to the administration site's CDN folder to enable publishing through the Content Delivery Network (CDN).")]
        public bool EnableCDNChildApplication { get; set; }

        public IEnumerable<System.Web.Mvc.SelectListItem> Certificates { get; set; }
    }

    public class BindingModel
    {
        public Guid Id { get; set; }
        
        // Web Site
        public Guid WebSiteId { get; set; }

        [Display(Name = "Web Site")]
        public string WebSiteName { get; set; }

        // Binding
        [Required]
        [Display(Name = "Protocol", Description = "Specify the protocol for the site binding.")]
        public string Protocol { get; set; }

        [Required]
        [Display(Name = "Port", Description = "Type the port on which HTTP.sys must listen for requests made to this site. If you select HTTP protocol, the default port is 80; if you select HTTPS protocol, the default port is 443. If you specify a port different from the default ports, clients must specify the port number in requests to the server or they will not connect to the site.")]
        public int Port { get; set; }

        [Required]
        [Display(Name = "IP Address (* = All Unassigned)", Description = "Type an IP address that users can use to access this site. If you select All Unassigned (*), this site will respond to requests for all IP addresses on the port and optional host name that you specify for this site, unless another site on the server has a binding on the same port but with a specific IP address.")]
        public string IpAddress { get; set; }

        [Required]
        [Display(Name = "Host Name", Description = "Type a host name if you want to assign one or more host names, also known as domain names, to one computer that uses a single IP address. If you specify a host name, clients must use the host name instead of the IP address to access the site.")]
        public string HostName { get; set; }

        [Display(Name = "SSL certificate", Description = "Select the certificate that you want the site to use for SSL.")]
        public Guid? CertificateId { get; set; }

        public string CertificateName { get; set; }

        public string Url { get; set; }

        public IEnumerable<System.Web.Mvc.SelectListItem> Certificates { get; set; }
    }
}