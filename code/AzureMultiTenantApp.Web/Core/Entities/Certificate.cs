namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Entities
{
    using System;
    using System.Collections.Generic;

    public class Certificate
    {
        public Certificate()
            : this(Guid.NewGuid())
        { 
        }

        public Certificate(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; private set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public byte[] Content { get; set; }

        public string Password { get; set; }

        public IEnumerable<Binding> Bindings { get; set; }
    }
}