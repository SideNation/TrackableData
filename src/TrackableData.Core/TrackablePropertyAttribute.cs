using System;
using System.Linq;
using System.Reflection;

namespace TrackableData
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TrackablePropertyAttribute : Attribute
    {
        public string[] Parameters { get; }

        public TrackablePropertyAttribute(params string[] parameters)
        {
            Parameters = parameters;
        }

        public string? this[string parameter]
        {
            get
            {
                if (parameter.EndsWith(":"))
                {
                    var p = Parameters.FirstOrDefault(x => x.StartsWith(parameter));
                    return p?.Substring(parameter.Length);
                }
                else
                {
                    return Parameters.Any(p => p == parameter) ? "true" : null;
                }
            }
        }

        public static string? GetParameter(ICustomAttributeProvider provider, string parameter)
        {
            if (parameter.EndsWith(":"))
            {
                foreach (var property in provider.GetCustomAttributes(false).OfType<TrackablePropertyAttribute>())
                {
                    var p = property.Parameters.FirstOrDefault(x => x.StartsWith(parameter));
                    if (p != null)
                        return p.Substring(parameter.Length);
                }
            }
            else
            {
                foreach (var property in provider.GetCustomAttributes(false).OfType<TrackablePropertyAttribute>())
                {
                    if (property.Parameters.Any(p => p == parameter))
                        return "true";
                }
            }
            return null;
        }
    }
}
