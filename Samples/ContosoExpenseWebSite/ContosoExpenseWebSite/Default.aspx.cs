namespace ContosoExpenseWebSite
{
    using System;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.RoleInstanceLabel.Text = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : string.Empty;
        }
    }
}