namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;
    using System.Collections.Generic;

    public class WebSite
    {
        public WebSite()
            : this(Guid.NewGuid())
        { 
        }

        public WebSite(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; private set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool EnableCDNChildApplication { get; set; }
 
        public bool EnableTestChildApplication { get; set; }

        public IEnumerable<Binding> Bindings { get; set; }
    }
}