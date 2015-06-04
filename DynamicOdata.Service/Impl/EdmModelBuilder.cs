using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DatabaseSchemaReader;
using DatabaseSchemaReader.DataSchema;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Impl
{
    
    public class EdmModelBuilder : IEdmModelBuilder
    {
        /// <summary>
        /// Cache model to boost performance
        /// </summary>
        private static readonly IDictionary<string, EdmModel> ModelMap = new Dictionary<string, EdmModel>();

        private readonly string _clientName;

        private readonly IDatabaseReader _databaseReader;

        public EdmModelBuilder(string clientName)
        {
            _clientName = clientName;

            var connectionString = ConfigurationManager.ConnectionStrings[_clientName].ConnectionString;
            _databaseReader = new DatabaseReader(connectionString, "System.Data.SqlClient");
        }

        private EdmStructuralProperty BuildProperty(EdmEntityType entity, DatabaseColumn column)
        {
            EdmPrimitiveTypeKind typeKind = EdmPrimitiveTypeKind.String;

            var stringTypes = new[] { "char", "nchar", "varchar", "nvarchar", "text", "ntext" };
            if (stringTypes.Contains(column.DbDataType))
                typeKind = EdmPrimitiveTypeKind.String;

            if (column.DbDataType == "tinyint")
                typeKind = EdmPrimitiveTypeKind.Byte;

            if (column.DbDataType == "smallint")
                typeKind = EdmPrimitiveTypeKind.Int16;

            if (column.DbDataType == "int")
                typeKind = EdmPrimitiveTypeKind.Int32;

            if (column.DbDataType == "bigint")
                typeKind = EdmPrimitiveTypeKind.Int64;

            if (column.DbDataType == "float")
                typeKind = EdmPrimitiveTypeKind.Double;

            if (column.DbDataType == "real")
                typeKind = EdmPrimitiveTypeKind.Single;

            var decimalTypes = new[] { "decimal", "numeric", "money", "smallmoney" };
            if (decimalTypes.Contains(column.DbDataType))
                typeKind = EdmPrimitiveTypeKind.Decimal;

            if (column.DbDataType == "datetime" || column.DbDataType == "smalldatetime" || column.DbDataType == "date")
                typeKind = EdmPrimitiveTypeKind.DateTime;

            if (column.DbDataType == "time")
                typeKind = EdmPrimitiveTypeKind.DateTimeOffset;

            if (column.DbDataType == "timestamp")
                typeKind = EdmPrimitiveTypeKind.DateTimeOffset;

            if (column.DbDataType == "uniqueidentifier")
                typeKind = EdmPrimitiveTypeKind.Guid;

            if (column.DbDataType == "geography")
                typeKind = EdmPrimitiveTypeKind.Geography;

            if (column.DbDataType == "bit")
                typeKind = EdmPrimitiveTypeKind.Boolean;

            if (column.DbDataType == "binary")
                typeKind = EdmPrimitiveTypeKind.Binary;

            return entity.AddStructuralProperty(column.Name, typeKind, column.Nullable);
        }

        private EdmEntityType BuildEdmEntityType(DatabaseTable table)
        {
            EdmEntityType entity = new EdmEntityType(table.SchemaOwner, table.Name);

            foreach (var column in table.Columns)
            {
                var property = BuildProperty(entity, column);

                if (column.IsPrimaryKey)
                    entity.AddKeys(property);
            }

            return entity;
        }

        public EdmModel GetModel()
        {
            EdmModel model;

            if (!ModelMap.TryGetValue(_clientName, out model))
            {
                model = DoGetModel();
                ModelMap.Add(_clientName, model);
            }

            return model;
        }

        private EdmModel DoGetModel()
        {
            EdmModel model = new EdmModel();
            EdmEntityContainer container = new EdmEntityContainer("ns", "container");
            model.AddElement(container);

            var schema = _databaseReader.ReadAll();

            foreach (var table in schema.Tables)
            {
                var entity = BuildEdmEntityType(table);
                model.AddElement(entity);

                var entitySetName = entity.Name.Replace(" ", ""); // entity set's name should not have space inside 
                container.AddEntitySet(entitySetName, entity);
            }

            return model;
        }
    }
}