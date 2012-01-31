namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Web.Mvc;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models;

    [Authorize]
    public class WebSiteController : Controller
    {
        private readonly IWebSiteRepository webSiteRepository;

        public WebSiteController()
            : this(new WebSiteRepository())
        {
        }

        public WebSiteController(IWebSiteRepository webSiteRepository)
        {
            this.webSiteRepository = webSiteRepository;
        }

        public ActionResult Index()
        {
            var webSites = this.webSiteRepository.RetrieveWebSitesWithBindings();
            var model = webSites.Select(
                s => new WebSiteModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Url = this.GetDefaultBindingUrl(s),
                    TestUrl = this.GetDefaultBindingTestUrl(s)
                });
            
            return View("List", model);
        }

        public ActionResult Edit(Guid id)
        {
            var website = this.webSiteRepository.RetrieveWebSiteWithBindings(id);
            var model = new WebSiteModel
            {
                Id = website.Id,
                Name = website.Name,
                Description = website.Description,
                EnableTestChildApplication = website.EnableTestChildApplication,
                EnableCDNChildApplication = website.EnableCDNChildApplication,
                Bindings = website.Bindings.Select(b => new BindingModel
                {
                    Id = b.Id,
                    CertificateId = b.CertificateId,
                    CertificateName = b.Certificate != null ? b.Certificate.Name : string.Empty,
                    HostName = b.HostName,
                    IpAddress = b.IpAddress,
                    Port = b.Port,
                    Protocol = b.Protocol,
                    WebSiteId = b.WebSiteId,
                    Url = this.GetBindingUrl(b)
                })
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Guid id, WebSiteModel model)
        {
            try
            {
                var site = new WebSite(id)
                {
                    Name = model.Name,
                    Description = model.Description,
                    EnableTestChildApplication = model.EnableTestChildApplication,
                    EnableCDNChildApplication = model.EnableCDNChildApplication,
                };

                this.webSiteRepository.UpdateWebSite(site);

                return RedirectToAction("Index");
            }
            catch
            {
                return View(model);
            }
        }

        public ActionResult Create()
        {
            var model = new WebSiteCreateModel()
            {
                Protocol = "http",
                Port = 80,
                IpAddress = "*",
                //Certificates = this.GetCertificatesList(),
                EnableTestChildApplication = true,
                EnableCDNChildApplication = true
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Create(WebSiteCreateModel model)
        {
            try
            {
                if (!this.ValidateDuplicatedSites(model.Name))
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }

                if (!this.ValidateDuplicatedBinding(model.HostName, model.Protocol, model.Port))
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }

                if (this.ValidateCertificateAndPort(model.CertificateId, model.Port, model.Protocol))
                {
                    var webSite = new WebSite()
                    {
                        Name = model.Name.Replace(" ", string.Empty).ToLowerInvariant(),
                        Description = model.Description,
                        EnableCDNChildApplication = model.EnableCDNChildApplication,
                        EnableTestChildApplication = model.EnableTestChildApplication
                    };

                    var binding = new Binding()
                    {
                        Port = model.Port,
                        Protocol = model.Protocol,
                        HostName = model.HostName,
                        IpAddress = model.IpAddress,
                        CertificateId = model.CertificateId
                    };

                    this.webSiteRepository.CreateWebSiteWithBinding(webSite, binding);

                    return RedirectToAction("Index");
                }
                else
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }
            }
            catch
            {
                //model.Certificates = this.GetCertificatesList();
                return View(model);
            }
        }

        public ActionResult Delete(Guid id)
        {
            this.webSiteRepository.RemoveWebSite(id);

            return RedirectToAction("Index");
        }

        public ActionResult CreateBinding(Guid webSiteId)
        {
            var site = this.webSiteRepository.RetrieveWebSite(webSiteId);

            var model = new BindingModel()
            {
                WebSiteId = webSiteId,
                WebSiteName = site.Name,
                Protocol = "http",
                Port = 80,
                IpAddress = "*",
                CertificateId = null,
                //Certificates = this.GetCertificatesList()
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateBinding(Guid webSiteId, BindingModel model)
        {
            try
            {
                if (!this.ValidateDuplicatedBinding(model.HostName, model.Protocol, model.Port))
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }

                if (this.ValidateCertificateAndPort(model.CertificateId, model.Port, model.Protocol))
                {
                    var binding = new Binding()
                    {
                        Port = model.Port,
                        Protocol = model.Protocol,
                        HostName = model.HostName,
                        IpAddress = model.IpAddress,
                        CertificateId = model.CertificateId
                    };

                    this.webSiteRepository.AddBindingToWebSite(webSiteId, binding);

                    return RedirectToAction("Edit", new { id = webSiteId });
                }
                else
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }
            }
            catch
            {
                //model.Certificates = this.GetCertificatesList();
                return View(model);
            }
        }

        public ActionResult EditBinding(Guid id)
        {
            var binding = this.webSiteRepository.RetrieveBinding(id);
            var site = this.webSiteRepository.RetrieveWebSite(binding.WebSiteId);

            var model = new BindingModel()
            {
                WebSiteId = id,
                WebSiteName = site.Name,
                Protocol = binding.Protocol,
                IpAddress = binding.IpAddress,
                Port = binding.Port,
                HostName = binding.HostName,
                CertificateId = binding.CertificateId,
                //Certificates = this.GetCertificatesList()
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EditBinding(Guid id, BindingModel model)
        {
            try
            {
                if (!this.ValidateDuplicatedBinding(model.HostName, model.Protocol, model.Port))
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }

                if (this.ValidateCertificateAndPort(model.CertificateId, model.Port, model.Protocol))
                {
                    Binding binding = this.webSiteRepository.RetrieveBinding(id);
                    binding.Port = model.Port;
                    binding.Protocol = model.Protocol;
                    binding.HostName = model.HostName;
                    binding.IpAddress = model.IpAddress;
                    binding.CertificateId = model.CertificateId;

                    this.webSiteRepository.UpdateBinding(binding);

                    return RedirectToAction("Edit", new { id = binding.WebSiteId });
                }
                else
                {
                    //model.Certificates = this.GetCertificatesList();
                    return View(model);
                }
            }
            catch
            {
                //model.Certificates = this.GetCertificatesList();
                return View(model);
            }
        }

        public ActionResult DeleteBinding(Guid id)
        {
            Binding binding = this.webSiteRepository.RetrieveBinding(id);
            this.webSiteRepository.RemoveBinding(id);

            return RedirectToAction("Edit", new { id = binding.WebSiteId });
        }

        private bool ValidateCertificateAndPort(Guid? certificateId, int port, string protocol)
        {
            if (protocol.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (!certificateId.HasValue)
                {
                    ModelState.AddModelError("CertificateId", "You must select an SSL certificate to create an https binding.");
                    return false;
                }
                else
                {
                    foreach (var bind in this.webSiteRepository.RetrieveBindingsForPort(port))
                    {
                        if (bind.CertificateId != certificateId)
                        {
                            ModelState.AddModelError("CertificateId", "The selected port has a different SSL certificate already associated with it.");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private string GetDefaultBindingUrl(WebSite webSite)
        {
            var defaultBinding = webSite.Bindings.FirstOrDefault();
            if (defaultBinding == null)
            {
                return null;
            }

            return this.GetUrl(defaultBinding.Protocol, defaultBinding.HostName, defaultBinding.Port);
        }

        private string GetDefaultBindingTestUrl(WebSite webSite)
        {
            if (!webSite.EnableTestChildApplication)
            {
                return null;
            }

            var defaultBinding = webSite.Bindings.FirstOrDefault();
            if (defaultBinding == null)
            {
                return null;
            }

            return this.GetTestUrl(Request.Url.Scheme + "://" + Request.Url.Authority, webSite.Name);
        }

        private string GetBindingUrl(Binding b)
        {
            return this.GetUrl(b.Protocol, b.HostName, b.Port);
        }

        private string GetUrl(string protocol, string hostName, int port)
        {
            return string.Format(
                "{0}://{1}{2}",
                protocol,
                hostName,
                (port <= 0 | port == 80 ? string.Empty : ":" + port.ToString(CultureInfo.InvariantCulture)));
        }

        private string GetTestUrl(string adminSiteUrl, string siteName)
        {
            return string.Format("{0}/test/{1}", adminSiteUrl, siteName);
        }

        private bool ValidateDuplicatedBinding(string hostname, string protocol, int port)
        {
            foreach (var bind in this.webSiteRepository.RetrieveBindingsForPort(port))
            {
                if (bind.HostName == hostname && bind.Protocol == protocol)
                {
                    ModelState.AddModelError(string.Empty, "The combination of hostname, protocol and port is already in use.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateDuplicatedSites(string siteName)
        {
            foreach (var sites in this.webSiteRepository.RetrieveWebSites())
            {
                if (sites.Name == siteName)
                {
                    ModelState.AddModelError(string.Empty, string.Format("The IIS Site Name '{0}' is already in use.", siteName));
                    return false;
                }
            }
            return true;

        }
    }
}