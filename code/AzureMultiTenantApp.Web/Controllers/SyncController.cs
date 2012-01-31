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
        private readonly ILogRepository logRepository;
        private readonly ISyncStatusRepository syncStatusRepository;

        public SyncController()
            : this(new LogRepository(), new SyncStatusRepository())
        {
        }

        public SyncController(ILogRepository logRepository, ISyncStatusRepository syncStatusRepository)
        {
            this.logRepository = logRepository;
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

        public ActionResult Log(int? count, string level)
        {
            var take = count.HasValue ? count.Value : 100;
            var currentLevel = level == null || level.Equals("All", StringComparison.OrdinalIgnoreCase) ? string.Empty : level;

            var logMessages = this.logRepository.RetrieveLogMessages(take, currentLevel);
            var model = logMessages.Select(l => new LogMessageModel
            {
                Level = l.Level,
                LogTimestamp = l.LogTimestamp,
                Message = l.Message,
                RoleInstanceId = l.RoleInstanceId
            });

            var levels = new List<SelectListItem>
            {
                new SelectListItem { Text = "Error" },
                new SelectListItem { Text = "Information" },
                new SelectListItem { Text = "Warning" }
            };

            if (!string.IsNullOrWhiteSpace(currentLevel))
            {
                foreach (var l in levels)
                {
                    if (l.Text.Equals(currentLevel, StringComparison.OrdinalIgnoreCase))
                    {
                        l.Selected = true;
                        break;
                    }
                }
            }

            ViewBag.Count = take;
            ViewBag.Levels = levels;
            
            return View("Log", model);
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