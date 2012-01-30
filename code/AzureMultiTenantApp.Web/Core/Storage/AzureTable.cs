namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Extensions;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class AzureTable<T> : IAzureTable<T> where T : TableServiceEntity, new()
    {
        private readonly string tableName;
        private readonly CloudStorageAccount account;

        public AzureTable()
            : this(CloudStorageAccount.DevelopmentStorageAccount)
        {
        }

        public AzureTable(CloudStorageAccount account)
            : this(account, typeof(T).Name)
        {
        }

        public AzureTable(CloudStorageAccount account, string tableName)
        {
            this.tableName = tableName;
            this.account = account;
        }

        public IQueryable<T> Query
        {
            get
            {
                TableServiceContext context = this.CreateContext();
                return context.CreateQuery<T>(this.tableName).AsTableServiceQuery();
            }
        }

        public bool CreateIfNotExist()
        {
            var cloudTableClient = new CloudTableClient(this.account.TableEndpoint.ToString(), this.account.Credentials);
            return cloudTableClient.CreateTableIfNotExist<T>(this.tableName);
        }

        public bool DeleteIfExist()
        {
            var cloudTableClient = new CloudTableClient(this.account.TableEndpoint.ToString(), this.account.Credentials);
            return cloudTableClient.DeleteTableIfExist(this.tableName);
        }

        public void AddEntity(T obj)
        {
            this.AddEntity(new[] { obj });
        }

        public void AddEntity(IEnumerable<T> objs)
        {
            TableServiceContext context = this.CreateContext();

            foreach (var obj in objs)
            {
                context.AddObject(this.tableName, obj);
            }

            var saveChangesOptions = SaveChangesOptions.None;
            if (objs.Distinct(new PartitionKeyComparer()).Count() == 1)
            {
                saveChangesOptions = SaveChangesOptions.Batch;
            }

            context.SaveChanges(saveChangesOptions);
        }

        public void AddOrUpdateEntity(T obj)
        {
            this.AddOrUpdateEntity(new[] { obj });
        }

        public void AddOrUpdateEntity(IEnumerable<T> objs)
        {
            foreach (var obj in objs)
            {
                var pk = obj.PartitionKey;
                var rk = obj.RowKey;
                T existingObj = null;

                try
                {
                    existingObj = (from o in this.Query
                                   where o.PartitionKey == pk && o.RowKey == rk
                                   select o).SingleOrDefault();
                }
                catch
                {
                }

                if (existingObj == null)
                {
                    this.AddEntity(obj);
                }
                else
                {
                    TableServiceContext context = this.CreateContext();
                    context.AttachTo(this.tableName, obj, "*");
                    context.UpdateObject(obj);
                    context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
                }
            }
        }

        public void DeleteEntity(T obj)
        {
            this.DeleteEntity(new[] { obj });
        }

        public void DeleteEntity(IEnumerable<T> objs)
        {
            TableServiceContext context = this.CreateContext();
            foreach (var obj in objs)
            {
                context.AttachTo(this.tableName, obj, "*");
                context.DeleteObject(obj);
            }

            try
            {
                context.SaveChanges();
            }
            catch (DataServiceRequestException ex)
            {
                var dataServiceClientException = ex.InnerException as DataServiceClientException;
                if (dataServiceClientException != null)
                {
                    if (dataServiceClientException.StatusCode == 404)
                    {
                        return;
                    }
                }

                throw;
            }
        }

        private TableServiceContext CreateContext()
        {
            var context = new TableServiceContext(this.account.TableEndpoint.ToString(), this.account.Credentials)
            {
                ResolveType = t => typeof(T),
                RetryPolicy = RetryPolicies.RetryExponential(RetryPolicies.DefaultClientRetryCount, RetryPolicies.DefaultClientBackoff)
            };

            return context;
        }

        private class PartitionKeyComparer : IEqualityComparer<TableServiceEntity>
        {
            public bool Equals(TableServiceEntity x, TableServiceEntity y)
            {
                return string.Compare(x.PartitionKey, y.PartitionKey, true, CultureInfo.InvariantCulture) == 0;
            }

            public int GetHashCode(TableServiceEntity obj)
            {
                return obj.PartitionKey.GetHashCode();
            }
        }
    }
}