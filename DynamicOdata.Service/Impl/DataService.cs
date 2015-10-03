using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Dapper;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Impl
{
    public class DataService : IDataService
    {
        private readonly string _connectionString;

        public DataService(string clientName)
        {
            _connectionString = ConfigurationManager.ConnectionStrings[clientName].ConnectionString;
        }

        private EdmEntityObject CreateEdmEntity(IEdmEntityType entityType, dynamic row)
        {
            if (row == null)
                return null;

            var entity = new EdmEntityObject(entityType);
            IDictionary<string, object> propertyMap = row as IDictionary<string, object>;

            if (propertyMap != null)
            {
                foreach (var propertyPair in propertyMap)
                    entity.TrySetPropertyValue(propertyPair.Key, propertyPair.Value);
            }

            return entity;
        }

        public int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions)
        {
            var entityType = collectionType.ElementType.Definition as EdmEntityType;
            int count = 0;

            if (entityType != null)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sqlBuilder = new SqlQueryBuilder(entityType, queryOptions);
                    count = connection.Query<int>(sqlBuilder.ToCountSql()).Single();
                }
            }

            return count;
        }

        public EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions oDataQueryOptions)
        {
            var entityType = collectionType.ElementType.Definition as EdmEntityType;
            var collection = new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType, true));

            if (entityType != null)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sqlBuilder = new SqlQueryBuilder(entityType, oDataQueryOptions);
                    IEnumerable<dynamic> rows = connection.Query<dynamic>(sqlBuilder.ToSql());

                    foreach (dynamic row in rows)
                    {
                        var entity = CreateEdmEntity(entityType, row);
                        collection.Add(entity);
                    }
                }
            }

            return collection;
        }

        public EdmEntityObject Get(string key, IEdmEntityType entityType)
        {
            var keys = entityType.DeclaredKey.ToList();

            // make sure entity type has unique key, not composite key
            if (keys.Count != 1)
                return null;

            var sql = $@"SELECT * FROM [{entityType.Namespace}].[{entityType.Name}] WHERE [{keys.First().Name}] = @Key";

            using (var connection = new SqlConnection(_connectionString))
            {
                var row = connection.Query(sql, new
                {
                    Key = key
                }).SingleOrDefault();

                var entity = CreateEdmEntity(entityType, row);
                return entity;
            }
        }

        public void Insert(IEdmEntityType entityType, IEdmEntityObject entity)
        {
            var values = new List<string>();

            foreach (var property in entityType.DeclaredProperties)
            {
                object value;

                if (!entity.TryGetPropertyValue(property.Name, out value))
                    continue;

                var valueString = (value == null) ? "NULL" : $"'{value}'";
                values.Add(valueString);
            }

            var sql = $@"INSERT INTO [{entityType.Namespace}].[{entityType.Name}] VALUES ({string.Join(",", values)})";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql);
            }
        }

        public void Update(IEdmEntityType entityType, IEdmEntityObject entity, string key)
        {
            var keys = entityType.DeclaredKey.ToList();

            // make sure entity type has unique key, not composite key
            if (keys.Count != 1)
                return;

            var valuePairs = new List<string>();

            foreach (var property in entityType.DeclaredProperties)
            {
                object value;

                if (!entity.TryGetPropertyValue(property.Name, out value))
                    continue;

                var valueString = (value == null) ? "NULL" : $"'{value}'";
                string pairString = $"{property.Name}={valueString}";

                valuePairs.Add(pairString);
            }

            var sql =
                $@"UPDATE [{entityType.Namespace}].[{entityType.Name}] SET {string.Join(",", valuePairs)} WHERE [{
                    keys.First().Name}] = @key";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql, new { Key = key });
            }
        }

        public void Delete(IEdmEntityType entityType, string key)
        {
            var keys = entityType.DeclaredKey.ToList();

            // make sure entity type has unique key, not composite key
            if (keys.Count != 1)
                return;

            using (var connection = new SqlConnection(_connectionString))
            {
                var sql =
                    $@"DELETE FROM [{entityType.Namespace}].[{entityType.Name}] WHERE [{keys.First().Name}] = @Key";

                connection.Execute(sql, new { Key = key });
            }
        }
    }
}