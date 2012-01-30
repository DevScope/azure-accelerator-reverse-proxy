namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Controllers
{
    using System;
    using System.Web.Mvc;
    using System.Web.Security;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Models;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class AccountController : Controller
    {
        public ActionResult LogOn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (this.ModelState.IsValid)
            {
                if (ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "WebSite");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "WebSite");
        }

        private static bool ValidateUser(string userName, string password)
        {
            return RoleEnvironment.GetConfigurationSettingValue("AdminUserName").Equals(userName, StringComparison.OrdinalIgnoreCase) &&
                   RoleEnvironment.GetConfigurationSettingValue("AdminUserPassword").Equals(password);
        }
    }
}