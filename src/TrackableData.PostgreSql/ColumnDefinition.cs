using System;

namespace TrackableData.PostgreSql
{
    public class ColumnDefinition
    {
        public string Name { get; }
        public Type Type { get; }
        public int Length { get; }

        public ColumnDefinition(string name, Type type = null, int length = 0)
        {
            Name = name;
            Type = type;
            Length = length;
        }
    }
}
