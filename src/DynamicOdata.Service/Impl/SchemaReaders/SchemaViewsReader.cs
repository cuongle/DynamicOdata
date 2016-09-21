using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using DynamicOdata.Service.Models;

namespace DynamicOdata.Service.Impl.SchemaReaders
{
  public class SchemaViewsReader : ISchemaReader
  {
    private readonly string _connectionString;
    private readonly string _schemaName;

    public SchemaViewsReader(string databaseConnectionString, string schemaName)
    {
      if (String.IsNullOrWhiteSpace(databaseConnectionString))
      {
        throw new ArgumentException("Argument is null or whitespace", nameof(databaseConnectionString));
      }

      if (String.IsNullOrWhiteSpace(schemaName))
      {
        throw new ArgumentException("Argument is null or whitespace", nameof(schemaName));
      }

      _schemaName = schemaName;
      _connectionString = databaseConnectionString;
    }

    public IEnumerable<DatabaseTable> GetTables(IEnumerable<TableInfo> tableInfos)
    {
      if (tableInfos != null)
      {
        throw new NotSupportedException("Filtering by views is not supported.");
      }

      var columns = GetColumns();
      List<DatabaseTable> tables = new List<DatabaseTable>();

      foreach (var schema in columns.GroupBy(c => c.Schema))
      {
        var tableList = schema.GroupBy(c => c.Table).Select(
          tableGroup => new DatabaseTable()
          {
            Schema = schema.Key,
            Name = tableGroup.Key,
            Columns = tableGroup.AsEnumerable()
          });

        tables.AddRange(tableList);
      }

      return tables;
    }

    private CommandDefinition BuildSql()
    {
      string sql = @"
                SELECT
                    schema_name(t.schema_id) as [Schema],
		                t.name as [Table],
		                c.name as Name,
		                c.is_identity as IsPrimaryKey,
		                c.is_nullable as Nullable,
		                lower(ty.name) as DataType
                FROM sys.columns c
                INNER JOIN sys.views t ON t.object_id = c.object_id
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            ";

      DynamicParameters commandParameters = new DynamicParameters();

      sql += " WHERE schema_name(t.schema_id) = @schema";
      commandParameters.Add("@schema", _schemaName);

      var commandDefinition = new CommandDefinition(sql, commandParameters);

      return commandDefinition;
    }

    private IEnumerable<DatabaseColumn> GetColumns()
    {
      CommandDefinition cmd = BuildSql();

      using (var connection = new SqlConnection(_connectionString))
      {
        var databaseColumns = connection.Query<DatabaseColumn>(cmd);
        return databaseColumns;
      }
    }
  }
}