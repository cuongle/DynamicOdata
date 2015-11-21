using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using DynamicOdata.Service.Models;

namespace DynamicOdata.Service.Impl
{
    public class SchemaReader : ISchemaReader
    {
        private readonly string _connectionString;

        public SchemaReader(string clientName)
        {
            _connectionString = ConfigurationManager.ConnectionStrings[clientName].ConnectionString;
        }

        private string BuildSql(IEnumerable<TableInfo> tableInfos)
        {
            string sql = @"
                SELECT	schema_name(t.schema_id) as [Schema], 
		                t.name as [Table],  
		                c.name as Name, 
		                c.is_identity as IsPrimaryKey,
		                c.is_nullable as Nullable,
		                ty.name as DataType

                FROM sys.columns c 
                INNER JOIN sys.tables t ON t.object_id = c.object_id 
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            ";

            if (tableInfos != null && tableInfos.Any())
            {
                var pairClauses = tableInfos.Select(
               info => $"(schema_name(t.schema_id) = '{info.Schema}' AND t.name = '{info.Name}')");

                string whereClause = string.Join(" OR ", pairClauses);

                if (!string.IsNullOrEmpty(whereClause))
                    sql += " WHERE " + whereClause;
            }

            return sql;
        }

        private IEnumerable<DatabaseColumn> GetColumns(IEnumerable<TableInfo> tableInfos)
        {
            string sql = BuildSql(tableInfos);

            using (var connection = new SqlConnection(_connectionString))
            {
                var databaseColumns = connection.Query<DatabaseColumn>(sql);
                return databaseColumns;
            }
        }

        public IEnumerable<DatabaseTable> GetTables(IEnumerable<TableInfo> tableInfos)
        {
            var columns = GetColumns(tableInfos);
            List<DatabaseTable> tables = new List<DatabaseTable>();

            foreach (var schema in columns.GroupBy(c => c.Schema))
            {
                var tableList = schema.GroupBy(c => c.Table).Select(tableGroup => new DatabaseTable()
                {
                    Schema = schema.Key,
                    Name = tableGroup.Key,
                    Columns = tableGroup.AsEnumerable()
                });

                tables.AddRange(tableList);
            }

            return tables;
        }
    }
}
