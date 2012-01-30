namespace Microsoft.Samples.DPE.AzureMultiTenantApp.Web.Core.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Text;

    public class TraceHelper
    {
        private static TraceSource Trace;

        static TraceHelper()
        {
            Trace = new TraceSource("default", SourceLevels.Information);
        }

        public static void Configure(SourceLevels sourceLevels)
        {
            Trace.Switch.Level = sourceLevels;
            Trace.Listeners.Add(new AzureTableTraceListener { Filter = new EventTypeFilter(sourceLevels) });
        }

        public static void TraceVerbose(string format, params object[] args)
        {
            Trace.TraceEvent(TraceEventType.Verbose, 0, format, args);
        }

        public static void TraceInformation(string format, params object[] args)
        {
            Trace.TraceEvent(TraceEventType.Information, 0, format, args);
        }

        public static void TraceWarning(string format, params object[] args)
        {
            Trace.TraceEvent(TraceEventType.Warning, 0, format, args);
        }

        public static void TraceError(string format, params object[] args)
        {
            Trace.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        public void TraceException(Exception exception, string format, params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(format, args);
            sb.Append(' ');
            sb.Append(exception.Message);
            sb.AppendLine();
            sb.Append(exception.StackTrace);
            if (exception is AggregateException)
            {
                ((AggregateException)exception).Flatten().Handle((innerException) =>
                {
                    while (innerException != null)
                    {
                        sb.AppendLine();
                        sb.Append("* ");
                        sb.AppendLine(innerException.Message);
                        sb.AppendLine(innerException.StackTrace);
                        innerException = innerException.InnerException;
                    }

                    return true;
                });
            }

            Trace.TraceEvent(TraceEventType.Error, 0, sb.ToString());
        }
    }
}
