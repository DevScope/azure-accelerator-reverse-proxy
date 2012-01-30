namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.IO;
    using System.Web.Script.Serialization;
    using System.Xml.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class AzureBlobContainer<T> : IAzureBlobContainer<T>
    {
        private readonly CloudStorageAccount account;
        private readonly CloudBlobContainer container;

        public AzureBlobContainer(CloudStorageAccount account)
            : this(account, typeof(T).Name.ToLowerInvariant())
        {
        }

        public AzureBlobContainer(CloudStorageAccount account, string containerName)
        {
            this.account = account;

            var client = this.account.CreateCloudBlobClient();
            client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));

            this.container = client.GetContainerReference(containerName.ToLowerInvariant());
        }

        public void EnsureExist()
        {
            this.container.CreateIfNotExist();
        }

        public void EnsureExist(bool publicContainer)
        {
            this.container.CreateIfNotExist();
            var permissions = new BlobContainerPermissions();

            if (publicContainer)
            {
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            }

            this.container.SetPermissions(permissions);
        }

        public void Save(string objId, T obj)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            blob.Properties.ContentType = "application/json";
            var serializer = new JavaScriptSerializer();
            blob.UploadText(serializer.Serialize(obj));
        }

        public void SaveAsXml(string objId, T obj)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            blob.Properties.ContentType = "text/xml";
            var serializer = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                blob.UploadText(writer.ToString());
            }
        }

        public string SaveFile(string objId, byte[] content, string contentType)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            blob.Properties.ContentType = contentType;
            blob.UploadByteArray(content);
            return blob.Uri.ToString();
        }

        public string SaveFile(string objId, byte[] content, string contentType, TimeSpan timeOut)
        {
            TimeSpan currentTimeOut = this.container.ServiceClient.Timeout;
            this.container.ServiceClient.Timeout = timeOut;
            string result = this.SaveFile(objId, content, contentType);
            this.container.ServiceClient.Timeout = currentTimeOut;
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "If we dispose the stream the clien won't be able to use it")]
        public Stream GetFile(string objId)
        {
            Stream stream = new MemoryStream();
            CloudBlob blob = this.container.GetBlobReference(objId);
            blob.DownloadToStream(stream);
            stream.Seek(0, 0);
            return stream;
        }

        public byte[] GetBytes(string objId)
        {
            using (var stream = new MemoryStream())
            {
                try
                {
                    CloudBlob blob = this.container.GetBlobReference(objId);
                    blob.DownloadToStream(stream);
                    return stream.ToArray();
                }
                catch (StorageClientException)
                {
                    return null;
                }
            }
        }

        public T Get(string objId)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            try
            {
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<T>(blob.DownloadText());
            }
            catch (StorageClientException)
            {
                return default(T);
            }
        }

        public T GetFromXml(string objId)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StringReader(blob.DownloadText()))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (StorageClientException)
            {
                return default(T);
            }
        }

        public void Delete(string objId)
        {
            CloudBlob blob = this.container.GetBlobReference(objId);
            blob.DeleteIfExists();
        }
    }
}