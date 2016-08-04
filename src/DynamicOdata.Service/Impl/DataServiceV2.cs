using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Dapper;
using DynamicOdata.Service.Impl.ResultTransformers;
using DynamicOdata.Service.Impl.SqlBuilders;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Impl
{
  public class DataServiceV2 : IDataService
  {
    private readonly string _connectionString;
    private readonly IResultTransformer _resultTransformer;
    private readonly ISqlQueryBuilder _sqlQueryBuilder;

    public DataServiceV2(string databaseConnectionString, ISqlQueryBuilder sqlQueryBuilder, IResultTransformer resultTransformer)
    {
      if (String.IsNullOrWhiteSpace(databaseConnectionString))
      {
        throw new ArgumentException("Argument is null or whitespace", nameof(databaseConnectionString));
      }

      if (sqlQueryBuilder == null)
      {
        throw new ArgumentNullException(nameof(sqlQueryBuilder));
      }

      if (resultTransformer == null)
      {
        throw new ArgumentNullException(nameof(resultTransformer));
      }

      _connectionString = databaseConnectionString;
      _sqlQueryBuilder = sqlQueryBuilder;
      _resultTransformer = resultTransformer;
    }

    public int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions)
    {
      var entityType = collectionType.ElementType.Definition as EdmEntityType;
      int count = 0;

      if (entityType != null)
      {
        using (var connection = new SqlConnection(_connectionString))
        {
          var countSql = _sqlQueryBuilder.ToCountSql(queryOptions);
          var commandDefinition = new CommandDefinition(countSql.Query, countSql.Parameters, commandType: CommandType.Text);

          count = connection.Query<int>(commandDefinition).Single();
        }
      }

      return count;
    }

    public EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions queryOptions)
    {
      var sql = _sqlQueryBuilder.ToSql(queryOptions);

      using (var connection = new SqlConnection(_connectionString))
      {
        var cmd = new CommandDefinition(sql.Query, sql.Parameters, commandType: CommandType.Text);

        IEnumerable<IDictionary<string, object>> rows =
          connection
          .Query<dynamic>(cmd)
          .Cast<IDictionary<string, object>>();

        var collection = _resultTransformer.Translate(rows, collectionType);

        return collection;
      }
    }

    public EdmEntityObject Get(string key, IEdmEntityType entityType)
    {
      var keys = entityType.DeclaredKey.ToList();

      // make sure entity type has unique key, not composite key
      if (keys.Count != 1)
      {
        return null;
      }

      var sql = $@"SELECT * FROM [{entityType.Namespace}].[{entityType.Name}] WHERE [{keys.First().Name}] = @Key";

      using (var connection = new SqlConnection(_connectionString))
      {
        var row = connection.Query(
          sql,
          new
          {
            Key = key
          }).SingleOrDefault();

        IDictionary<string, object> rows = row as IDictionary<string, object>;

        return _resultTransformer.Translate(rows, entityType);
      }
    }
  }
}