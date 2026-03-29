using System;
using System.Data.Common;

namespace TrackableData.PostgreSql
{
    public interface ISqlProvider
    {
        Func<object, string> GetConvertToSqlValueFunc(Type type);

        Func<object, object> GetConvertFromDbValueFunc(Type type);

        string EscapeName(string name);

        string BuildCreateTableSql(string tableName,
                                   ColumnProperty[] columns,
                                   ColumnProperty[] primaryKeys,
                                   bool dropIfExists);

        string BuildInsertIntoSql(string tableName,
                                  string columns,
                                  string values,
                                  ColumnProperty identity);

        DbCommand CreateDbCommand(string sql, DbConnection connection);
    }
}
