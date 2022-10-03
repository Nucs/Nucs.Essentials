#define TRACE

using System;
using System.Diagnostics;
using System.Text;

namespace Nucs {
    /// <summary>
    /// Thin wrapper around System.Diagnostics.Trace to add debug logger and match the usage 
    /// of Common logger's Debug, Info, Warn, Error.
    /// </summary>
    public static class Logging {
        [Conditional("DEBUG")]
        public static void Debug(string message) {
            System.Diagnostics.Trace.WriteLine(message);
        }

        [Conditional("DEBUG")]
        public static void Trace(string message) {
            System.Diagnostics.Trace.WriteLine("TRACE:" + message);
        }

        [Conditional("DEBUG")]
        public static void TraceFormat(string format, params object[] args) {
            System.Diagnostics.Trace.WriteLine(string.Format("TRACE:" + format, args));
        }

        public static void Info(string message) {
            System.Diagnostics.Trace.TraceInformation(message);
        }

        public static void InfoFormat(string format, params object[] args) {
            System.Diagnostics.Trace.TraceInformation(string.Format(format, args));
        }

        public static void Error(string message) {
            System.Diagnostics.Trace.TraceError(message);
        }

        public static void Error(string message, Exception ex) {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            CreateExceptionString(sb, ex);
            System.Diagnostics.Trace.TraceError(sb.ToString());
        }

        public static void Error(Exception ex) {
            StringBuilder sb = new StringBuilder();
            CreateExceptionString(sb, ex);
            System.Diagnostics.Trace.TraceError(sb.ToString());
        }

        public static void ErrorFormat(string format, params object[] args) {
            System.Diagnostics.Trace.TraceError(string.Format(format, args));
        }

        public static void Warn(string message) {
            System.Diagnostics.Trace.TraceWarning(message);
        }

        public static void WarnFormat(string format, params object[] args) {
            System.Diagnostics.Trace.TraceWarning(string.Format(format, args));
        }

        public static void Fatal(string message) {
            System.Diagnostics.Trace.TraceError("FATAL:" + message);
        }

        public static void TraceEntry() {
            StackTrace trace = new StackTrace();
            if (trace.FrameCount > 1) {
                string ns = trace.GetFrame(1).GetMethod().DeclaringType.Namespace;
                string typeName = trace.GetFrame(1).GetMethod().DeclaringType.Name;
                Trace(string.Format("Entering {0}.{1}.{2}", ns, typeName, trace.GetFrame(1).GetMethod().Name));
            }
        }

        public static void TraceExit() {
            StackTrace trace = new StackTrace();
            if (trace.FrameCount > 1) {
                string ns = trace.GetFrame(1).GetMethod().DeclaringType.Namespace;
                string typeName = trace.GetFrame(1).GetMethod().DeclaringType.Name;
                Trace(string.Format("Exiting {0}.{1}.{2}", ns, typeName, trace.GetFrame(1).GetMethod().Name));
            }
        }

        private static void CreateExceptionString(StringBuilder sb, Exception e) {
            sb.Append(e.GetType().FullName);
            sb.AppendFormat("\n\tMessage: {0}", e.Message);
            sb.AppendFormat("\n\tSource: {0}", e.Source);
            sb.AppendFormat("\n\tStacktrace: {0}", e.StackTrace);

            if (e.InnerException != null) {
                sb.Append("\nInnerException:");
                CreateExceptionString(sb, e.InnerException);
            }
        }
    }
}