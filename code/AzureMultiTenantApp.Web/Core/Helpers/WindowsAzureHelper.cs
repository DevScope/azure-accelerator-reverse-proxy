namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Helpers
{
    using System;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public static class WindowsAzureHelper
    {
        public static bool IsComputeEmulatorEnvironment
        {
            get
            {
                return RoleEnvironment.IsAvailable && RoleEnvironment.DeploymentId.StartsWith("deployment", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}