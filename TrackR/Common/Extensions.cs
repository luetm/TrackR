
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using TrackR.DeepCloning;
using System.Reflection;

namespace TrackR.Common
{
    /// <summary>
    /// All extensions in one place.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Performs string.Format() on a string object.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string F(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Performs string.IsNullOrWhitespace() on a string object.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        public static string ToUriParameter(this object value, string key)
        {
            const string format = "{0}={1}";
            if (value == null)
            {
                return format.F(key, "NULL");
            }

            if (value is DateTime)
            {
                return format.F(key, ((DateTime)value).ToString("yyyy-MM-dd"));
            }

            var array = value as Array;
            if (array != null)
            {
                var parameters = array
                    .Cast<object>()
                    .Select(o => o.ToUriParameter(key));
                return string.Join("&", parameters);
            }

            return format.F(key, value.ToString());
        }
        public static T Inject<T>(this object value) where T : new()
        {
            return (T)new T().InjectFrom<DeepCloneInjection>(value);
        }
        public static T Inject<T>(this object value, T destination)
        {
            return (T)destination.InjectFrom<DeepCloneInjection>(value);
        }

        /// <summary>
        /// Used deep injection to inject data into arbitrary objects.
        /// </summary>
        /// <typeparam name="T">Class to transform the enumerable into. Needs a parameterless constructor.</typeparam>
        /// <param name="source">Source</param>
        /// <returns></returns>
        public static IEnumerable<T> Inject<T>(this IEnumerable source) where T : new()
        {
            return source
                .Cast<object>()
                .Select(o => (T)new T().InjectFrom<DeepCloneInjection>(o))
                .ToList();
        }
    }
}
