namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics
{
    using System.Diagnostics;
    using System.Globalization;

    public abstract class SimplifiedTraceListener : TraceListener
    {
        protected SimplifiedTraceListener(string name)
            : base(name)
        {
        }

        public override void Write(string message)
        {
            this.FilterTraceEventCore(null, string.Empty, TraceEventType.Information, 0, message);
        }

        public override void WriteLine(string message)
        {
            this.Write(message);
        }

        public override sealed void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            this.FilterTraceEventCore(eventCache, source, eventType, id, message);
        }

        public override sealed void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            string message = format;
            if (args != null)
            {
                message = string.Format(CultureInfo.CurrentCulture, format, args);
            }

            this.FilterTraceEventCore(eventCache, source, eventType, id, message);
        }

        public override sealed void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            this.FilterTraceEventCore(eventCache, source, eventType, id, null);
        }

        public override sealed void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            this.FilterTraceDataCore(eventCache, source, eventType, id, data);
        }

        public override sealed void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            this.FilterTraceDataCore(eventCache, source, eventType, id, data);
        }

        protected virtual bool ShouldTrace(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            return
                !(this.Filter != null &&
                  !this.Filter.ShouldTrace(eventCache, source, eventType, id, formatOrMessage, args, data1, data));
        }

        protected virtual void FilterTraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (!this.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            this.TraceEventCore(eventCache, source, eventType, id, message);
        }

        protected virtual void FilterTraceDataCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            if (!this.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
            {
                return;
            }

            this.TraceDataCore(eventCache, source, eventType, id, data);
        }

        protected abstract void TraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message);

        protected abstract void TraceDataCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data);
    }
}