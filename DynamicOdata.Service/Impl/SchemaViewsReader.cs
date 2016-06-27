using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using Dapper;
using DynamicOdata.Service.Models;

namespace DynamicOdata.Service.Impl
{
  public class SchemaViewsReader : ISchemaReader
  {
    public const string DefaultSchemaName = "dbo";
    private readonly string _connectionString;
    private readonly string _schemaName;

    public SchemaViewsReader(string databaseConnenctionString)
      : this(databaseConnenctionString, DefaultSchemaName)
    {
    }

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
      var columns = GetColumns(tableInfos);
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

    private CommandDefinition BuildSql(IEnumerable<TableInfo> viewsFilter)
    {
      string sql = @"
                SELECT
                    schema_name(t.schema_id) as [Schema],
		                t.name as [Table],
		                c.name as Name,
		                c.is_identity as IsPrimaryKey,
		                c.is_nullable as Nullable,
		                ty.name as DataType
                FROM sys.columns c
                INNER JOIN sys.views t ON t.object_id = c.object_id
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            ";

      DynamicParameters commandParameters = new DynamicParameters();

      var viewsInfo = viewsFilter?.ToList();

      //TODO: add logging info about ignoring schema from ctor if filters are passed
      if (viewsInfo != null)
      {
        var count = viewsInfo.Count;

        var pairClauses = new List<string>();
        for (int i = 0; i < count; i++)
        {
          var info = viewsInfo[i];

          string schemaName = $"tableSchema_{i}";
          string tableName = $"tableName_{i}";

          pairClauses.Add($"(schema_name(t.schema_id) = @{schemaName} AND t.name = @{tableName})");

          commandParameters.Add(schemaName, info.Schema);
          commandParameters.Add(tableName, info.Name);
        }

        string whereClause = string.Join(" OR ", pairClauses);

        if (!string.IsNullOrEmpty(whereClause))
        {
          sql += " WHERE " + whereClause;
        }
      }
      else
      {
        sql += " WHERE schema_name(t.schema_id) = @schema";
        commandParameters.Add("@schema", _schemaName);
      }

      var commandDefinition = new CommandDefinition(sql, commandParameters);

      return commandDefinition;
    }

    private IEnumerable<DatabaseColumn> GetColumns(IEnumerable<TableInfo> onlySpecifiedViews)
    {
      CommandDefinition cmd = BuildSql(onlySpecifiedViews);

      using (var connection = new SqlConnection(_connectionString))
      {
        var databaseColumns = connection.Query<DatabaseColumn>(cmd);
        return databaseColumns;
      }
    }
  }
}