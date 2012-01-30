namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.StorageClient;

    public interface IAzureTable<TEntity> where TEntity : TableServiceEntity
    {
        IQueryable<TEntity> Query { get; }

        bool CreateIfNotExist();

        bool DeleteIfExist();

        void AddEntity(TEntity obj);

        void AddEntity(IEnumerable<TEntity> objs);

        void AddOrUpdateEntity(TEntity obj);

        void AddOrUpdateEntity(IEnumerable<TEntity> objs);

        void DeleteEntity(TEntity obj);

        void DeleteEntity(IEnumerable<TEntity> objs);
    }
}