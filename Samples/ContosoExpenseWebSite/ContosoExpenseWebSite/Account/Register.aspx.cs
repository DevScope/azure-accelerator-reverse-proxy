namespace ContosoExpenseWebSite.Account
{
    using System;
    using System.Web.Security;

    public partial class Register : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.RegisterUser.ContinueDestinationPageUrl = Request.QueryString["ReturnUrl"];
        }

        protected void RegisterUser_CreatedUser(object sender, EventArgs e)
        {
            FormsAuthentication.SetAuthCookie(this.RegisterUser.UserName, false);

            string continueUrl = this.RegisterUser.ContinueDestinationPageUrl;
            
            if (string.IsNullOrEmpty(continueUrl))
            {
                continueUrl = "~/";
            }

            Response.Redirect(continueUrl);
        }
    }
}