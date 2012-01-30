namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Controllers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models;

    [Authorize]
    public class CertificateController : Controller
    {
        private readonly ICertificateRepository certificateRepository;

        public CertificateController()
            : this(new CertificateRepository())
        {
        }

        public CertificateController(ICertificateRepository certificateRepository)
        {
            this.certificateRepository = certificateRepository;
        }

        public ActionResult Index()
        {
            var certificates = this.certificateRepository.RetrieveCertificates();
            var model = certificates.Select(
                c => new CertificateModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Password = c.Password,
                    Description = c.Description
                });

            return View("List", model);
        }

        public ActionResult Details(Guid id)
        {
            var certificate = this.certificateRepository.RetrieveCertificateWithBindings(id);
            var model = new CertificateModel
            {
                Id = certificate.Id,
                Name = certificate.Name,
                Password = certificate.Password,
                Description = certificate.Description,
                Bindings = certificate.Bindings.Select(b =>
                    new BindingModel
                    {
                        Id = b.Id,
                        CertificateId = b.CertificateId,
                        CertificateName = b.Certificate != null ? b.Certificate.Name : string.Empty,
                        HostName = b.HostName,
                        IpAddress = b.IpAddress,
                        Port = b.Port,
                        Protocol = b.Protocol,
                        WebSiteId = b.WebSiteId,
                        WebSiteName = b.WebSite != null ? b.WebSite.Name : string.Empty
                    })
            };

            return View(model);
        }

        public ActionResult Create()
        {
            var model = new CertificateCreateModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult Create(CertificateCreateModel model, HttpPostedFileBase file)
        {
            try
            {
                var certificate = new Certificate()
                {
                    Name = model.Name,
                    Description = model.Description,
                    Password = model.Password
                };

                if ((file == null) || string.IsNullOrEmpty(file.FileName))
                {
                    var msg = "A pfx certificate file must be specified";
                    ModelState.AddModelError("file", msg);
                    ViewBag.CertificateError = msg;

                    return View();
                }
                else if (!Path.GetExtension(file.FileName).Equals(".pfx", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = "Only pfx certificates can be uploaded";
                    ModelState.AddModelError("file", msg);
                    ViewBag.CertificateError = msg;

                    return View();
                }

                if (file.ContentLength > 0)
                {
                    byte[] buffer = new byte[file.ContentLength];
                    file.InputStream.Read(buffer, 0, file.ContentLength);
                    certificate.Content = buffer;
                }

                this.certificateRepository.CreateCertificate(certificate);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Delete(Guid id)
        {
            this.certificateRepository.RemoveCertificate(id);
            return RedirectToAction("Index");
        }
    }
}