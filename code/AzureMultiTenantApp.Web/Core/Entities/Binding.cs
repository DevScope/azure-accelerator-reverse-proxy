namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;

    public class Binding
    {
        public Binding()
            : this(Guid.NewGuid())
        {
        }

        public Binding(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; private set; }

        public Guid WebSiteId { get; set; }

        public WebSite WebSite { get; set; }

        public string Protocol { get; set; }

        public string IpAddress 
        {
            get
            {
                return String.IsNullOrWhiteSpace(this._ipAddress) ? "*" : this._ipAddress;
            }

            set
            {
                this._ipAddress = value;
            }
        }
        private string _ipAddress;

        public int Port { get; set; }

        public string HostName { get; set; }

        public Guid? CertificateId { get; set; }

        public Certificate Certificate { get; set; }
    }
}