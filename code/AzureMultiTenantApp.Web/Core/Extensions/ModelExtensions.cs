namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions
{
    using System;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public static class ModelExtensions
    {
        public static WebSite ToModel(this WebSiteRow row)
        {
            if (row == null)
            {
                return null;
            }

            return new WebSite(new Guid(row.RowKey))
            {
                Name = row.Name,
                Description = row.Description,
                EnableTestChildApplication = row.EnableTestChildApplication.GetValueOrDefault(),
                EnableCDNChildApplication = row.EnableCDNChildApplication.GetValueOrDefault()
            };
        }

        public static WebSiteRow ToRow(this WebSite model)
        {
            if (model == null)
            {
                return null;
            }

            return new WebSiteRow(model.Id)
            {
                Name = model.Name,
                Description = model.Description,
                EnableTestChildApplication = model.EnableTestChildApplication,
                EnableCDNChildApplication = model.EnableCDNChildApplication
            };
        }

        public static Binding ToModel(this BindingRow row)
        {
            if (row == null)
            {
                return null;
            }

            return new Binding(new Guid(row.RowKey))
            {
                Port = row.Port,
                Protocol = row.Protocol,
                HostName = row.HostName ?? string.Empty,
                IpAddress = row.IpAddress,
                WebSiteId = row.WebSiteId,
                CertificateId = row.CertificateId
            };
        }

        public static BindingRow ToRow(this Binding model)
        {
            if (model == null)
            {
                return null;
            }

            return new BindingRow(model.Id)
            {
                WebSiteId = model.WebSiteId,
                Protocol = model.Protocol,
                IpAddress = model.IpAddress,
                Port = model.Port,
                HostName = model.HostName,
                CertificateId = model.CertificateId
            };
        }

        public static Certificate ToModel(this CertificateRow row)
        {
            if (row == null)
            {
                return null;
            }

            return new Certificate(new Guid(row.RowKey))
            {
                Name = row.Name,
                Description = row.Description,
                Password = row.Password
            };
        }

        public static CertificateRow ToRow(this Certificate model)
        {
            if (model == null)
            {
                return null;
            }

            return new CertificateRow(model.Id)
            {
                Name = model.Name,
                Description = model.Description,
                Password = model.Password
            };
        }

        public static LogMessage ToModel(this AzureLogMessage row)
        {
            if (row == null)
            {
                return null;
            }

            return new LogMessage()
            {
                RoleInstanceId = row.RoleInstanceId,
                Level = row.Level,
                LogTimestamp = row.LogTimestamp,
                Message = row.Message
            };
        }

        public static SyncStatusRow ToRow(this SyncStatus model)
        {
            if (model == null)
            {
                return null;
            }

            var deploymentId = string.IsNullOrWhiteSpace(model.DeploymentId) ? RoleEnvironment.DeploymentId : model.DeploymentId;
            var roleInstanceId = string.IsNullOrWhiteSpace(model.RoleInstanceId) ? RoleEnvironment.CurrentRoleInstance.Id : model.RoleInstanceId;

            return new SyncStatusRow(deploymentId, roleInstanceId, model.SiteName)
            {
                Status = model.Status.ToString(),
                IsOnline = model.IsOnline
            };
        }

        public static SyncStatus ToModel(this SyncStatusRow row)
        {
            if (row == null)
            {
                return null;
            }

            return new SyncStatus()
            {
                DeploymentId = row.PartitionKey,
                RoleInstanceId = row.RoleInstanceId,
                SiteName = row.SiteName,
                Status = (SyncInstanceStatus)Enum.Parse(typeof(SyncInstanceStatus), row.Status),
                SyncTimestamp = row.Timestamp,
                IsOnline = row.IsOnline.GetValueOrDefault(true)
            };
        }
    }
}