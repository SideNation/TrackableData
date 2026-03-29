using System;
using System.Reflection;

namespace TrackableData.PostgreSql
{
    public class ColumnProperty
    {
        public string Name { get; }
        public string EscapedName { get; }
        public Type Type { get; }
        public int Length { get; }
        public bool IsIdentity { get; }
        public PropertyInfo PropertyInfo { get; }

        public Func<object, string> ConvertToSqlValue { get; }
        public Func<object, object> ConvertFromDbValue { get; }
        public Func<object, string> ExtractToSqlValue { get; }
        public Action<object, object> InstallFromDbValue { get; }

        public ColumnProperty(string name,
                              string escapedName,
                              Type type = null,
                              int length = 0,
                              bool isIdentity = false,
                              PropertyInfo propertyInfo = null,
                              Func<object, string> convertToSqlValue = null,
                              Func<object, object> convertFromDbValue = null,
                              Func<object, string> extractToSqlValue = null,
                              Action<object, object> installFromDbValue = null)
        {
            Name = name;
            EscapedName = escapedName;
            Type = type;
            Length = length;
            IsIdentity = isIdentity;
            PropertyInfo = propertyInfo;
            ConvertToSqlValue = convertToSqlValue;
            ConvertFromDbValue = convertFromDbValue;
            ExtractToSqlValue = extractToSqlValue;
            InstallFromDbValue = installFromDbValue;
        }
    }
}
