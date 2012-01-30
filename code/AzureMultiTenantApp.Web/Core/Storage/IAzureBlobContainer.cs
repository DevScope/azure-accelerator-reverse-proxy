namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Storage
{
    using System;
    using System.IO;

    public interface IAzureBlobContainer<T>
    {
        void EnsureExist();

        void EnsureExist(bool publicContainer);

        void Save(string objId, T obj);

        void SaveAsXml(string objId, T obj);

        string SaveFile(string objId, byte[] content, string contentType);

        string SaveFile(string objId, byte[] content, string contentType, TimeSpan timeOut);

        T Get(string objId);

        T GetFromXml(string objId);

        Stream GetFile(string objId);

        byte[] GetBytes(string objId);

        void Delete(string objId);
    }
}