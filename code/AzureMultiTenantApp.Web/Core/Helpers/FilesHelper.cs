namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Helpers
{
    using System;
    using System.IO;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using System.Diagnostics;

    public static class FilesHelper
    {
        public static void RemoveFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("Remove Folder Error{0}{1}", Environment.NewLine, e.TraceInformation());
                }
            }
        }
    }
}