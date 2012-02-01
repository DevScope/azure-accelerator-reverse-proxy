namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Services;

    [Authorize]
    public class SyncController : Controller
    {
        private readonly SyncStatusRepository syncStatusRepository;

        public SyncController()
            : this(new SyncStatusRepository())
        {
        }

        public SyncController(SyncStatusRepository syncStatusRepository)
        {
            this.syncStatusRepository = syncStatusRepository;
        }

        public ActionResult Index(string webSiteName)
        {
            if (string.IsNullOrWhiteSpace(webSiteName))
            {
                throw new ArgumentNullException("webSiteName");
            }

            var webSiteStatus = this.syncStatusRepository.RetrieveSyncStatus(webSiteName);
            var model = webSiteStatus
                .Where(s => s.IsOnline)
                .Select(s => new SyncStatusModel
                {
                    RoleInstanceId = s.RoleInstanceId,
                    Status = s.Status.ToString(),
                    SyncTimestamp = s.SyncTimestamp
                });

            this.ViewBag.WebSiteName = webSiteName;

            return View(model);
        }

        public ActionResult SyncChange(bool enable)
        {
            if (enable)
            {
                SyncService.SyncEnable();
            }
            else
            {
                SyncService.SyncDisable();
            }
            return RedirectToAction("Index", "WebSite");
        }
    }
}