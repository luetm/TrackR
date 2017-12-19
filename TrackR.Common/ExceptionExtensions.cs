using System;
using System.Data.Entity.Validation;
using System.Linq;

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

            if (err is DbEntityValidationException errValidation)
            {
                if (errValidation.EntityValidationErrors.Any())
                {
                    result += "EntityValidationErrors:\n";
                    foreach (var validationError in errValidation.EntityValidationErrors)
                    {
                        var entityInfo = validationError.Entry.Entity.ToString();
                        entityInfo = entityInfo.Contains("Id=") ? entityInfo : validationError.Entry.Entity.GetType().Name;
                        result += $"Entity: {entityInfo}\n";

                        foreach (var validationErrorError in validationError.ValidationErrors)
                        {
                            result += $"Property: {validationErrorError.PropertyName} - ErrorMessage: {validationErrorError.ErrorMessage}";
                        }
                    }
                }
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
