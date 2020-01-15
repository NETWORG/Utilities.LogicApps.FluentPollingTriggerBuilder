using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections;
using System.Linq;

namespace NETWORG.Utilities.LogicApps.FluentPollingTriggerBuilder
{
    public static class UrlHelpers
    {
        /// <summary>
        /// Serialize object into (url) query
        /// </summary>
        /// <param name="data">Data to be serialized</param>
        /// <returns>Query string with ? at the begining</returns>
        public static string ToQueryString(this object data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var builder = new QueryBuilder();

            // Get all properties on the object
            var properties = data.GetType().GetProperties()
                .Where(x => x.CanRead)
                .Where(x => x.GetValue(data, null) != null)
                .ToDictionary(x => x.Name, x => x.GetValue(data, null));

            // all single value properties on the object
            var single = properties
                .Where(x => !(x.Value is IEnumerable) || x.Value is string);

            // add all single values
            foreach (var pair in single)
            {
                builder.Add(pair.Key, ObjectToString(pair.Value));
            }

            // Get names for all IEnumerable properties (excl. string)
            var propertyNames = properties
                .Where(x => !(x.Value is string) && x.Value is IEnumerable)
                .Select(x => x.Key)
                .ToList();

            // Add all IEnumerable properties into a querry builder, but get their string value
            foreach (var key in propertyNames)
            {
                var valueType = properties[key].GetType();
                var valueElemType = valueType.IsGenericType
                    ? valueType.GetGenericArguments()[0]
                    : valueType.GetElementType();
                if (valueElemType.IsPrimitive || valueElemType == typeof(string))
                {
                    var enumerable = properties[key] as IEnumerable;
                    foreach (var o in enumerable)
                    {
                        var value = ObjectToString(o);
                        builder.Add(key, value);
                    }
                }
            }

            return builder.ToString();
        }

        private static string ObjectToString(object o)
        {
            switch (o)
            {
                case DateTime d:
                    return d.ToString("s");
                default:
                    return Uri.EscapeDataString(o.ToString());
            }
        }
    }
}
