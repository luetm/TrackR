using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.Common
{
    public static class ExceptionExtensions
    {
        public static string GenerateInfo(this Exception err, bool stacktrace = true)
        {
            if (err == null) return "";

            var result = "";
            result += $"Type: {err.GetType().Name}\n";
            result += $"Message: {err.Message}\n";

            var inner = err.InnerException;
            while (inner != null)
            {
                result += $"Inner-Type: {inner.GetType().Name}\n";
                result += $"Inner-Message: {inner.Message}\n";
                inner = inner.InnerException;
            }

            if (stacktrace)
            {
                result += "Stracktrace:\n";
                if (err.StackTrace != null)
                {
                    result += err.StackTrace;
                }
            }
            return result;
        }
    }
}
