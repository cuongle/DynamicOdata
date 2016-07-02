using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using DynamicOdata.Service.Models;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Impl.EdmBuilders
{
  public class EdmObjectChierarchyModelBuilder : IEdmModelBuilder
  {
    private readonly ISchemaReader _schemaReader;
    private readonly char _separator;
    private PluralizationService _pluralizationService;

    public EdmObjectChierarchyModelBuilder(ISchemaReader schemaReader)
      : this(schemaReader, '.')
    {
    }

    public EdmObjectChierarchyModelBuilder(ISchemaReader schemaReader, char separator)
    {
      if (schemaReader == null)
      {
        throw new ArgumentNullException(nameof(schemaReader));
      }

      if (separator <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(separator));
      }

      _schemaReader = schemaReader;
      _pluralizationService = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-US"));
      _separator = separator;
    }

    public EdmModel GetModel()
    {
      EdmModel model = new EdmModel();

      var databaseTables = _schemaReader.GetTables(null);

      foreach (var table in databaseTables)
      {
        EdmEntityContainer container = model.FindDeclaredEntityContainer("container") as EdmEntityContainer;

        if (container == null)
        {
          container = new EdmEntityContainer(table.Schema, "container");
          model.AddElement(container);
        }

        EdmEntityType entity = new EdmEntityType(table.Schema, table.Name);

        var properties = table.Columns.Where(x => !x.Name.Contains(_separator)).ToList();
        var components = BuildComponentMap(table.Name, string.Empty, table.Columns.Where(w =>w.Name.Contains(_separator)));

        // first add components and references to it
        AddComponents(model, entity, components);

        // add columns
        AddProperties(entity, properties);

        model.AddElement(entity);

        container.AddEntitySet(_pluralizationService.Pluralize(table.Name), entity);
      }

      return model;
    }

    private static void AddProperties(EdmEntityType entity, IEnumerable<DatabaseColumn> properties)
    {
      foreach (var databaseColumn in properties)
      {
        var addedProperty = AddPropertyToEntity(entity, databaseColumn);

        if (databaseColumn.IsPrimaryKey)
        {
          entity.AddKeys(addedProperty);
        }
      }
    }

    private static IEdmStructuralProperty AddPropertyToEntity(EdmStructuredType entity, DatabaseColumn column)
    {
      var typeKind = EdmPrimitiveTypeKind.String;
      var typeMap = BuildEdmTypeMap();

      if (typeMap.ContainsKey(column.DataType))
      {
        typeKind = typeMap[column.DataType];
      }

      return entity.AddStructuralProperty(column.Name, typeKind, column.Nullable);
    }

    private static IDictionary<string, EdmPrimitiveTypeKind> BuildEdmTypeMap()
    {
      //TODO: chceck if all types from SQL are here
      var map = new Dictionary<string, EdmPrimitiveTypeKind>
      {
        { "tinyint", EdmPrimitiveTypeKind.Byte },
        { "smallint", EdmPrimitiveTypeKind.Int16 },
        { "int", EdmPrimitiveTypeKind.Int32 },
        { "bigint", EdmPrimitiveTypeKind.Int64 },
        { "float", EdmPrimitiveTypeKind.Double },
        { "real", EdmPrimitiveTypeKind.Single },
        { "uniqueidentifier", EdmPrimitiveTypeKind.Guid },
        { "geography", EdmPrimitiveTypeKind.Geography },
        { "bit", EdmPrimitiveTypeKind.Boolean },
        { "binary", EdmPrimitiveTypeKind.Binary }
      };

      var stringTypes = new[] { "char", "nchar", "varchar", "nvarchar", "text", "ntext" }
        .ToDictionary(s => s, _ => EdmPrimitiveTypeKind.String);

      var decimalTypes = new[] { "decimal", "numeric", "money", "smallmoney" }
        .ToDictionary(s => s, _ => EdmPrimitiveTypeKind.Decimal);

      var dateTimeTypes = new[] { "datetime", "smalldatetime", "date" }
        .ToDictionary(s => s, _ => EdmPrimitiveTypeKind.DateTime);

      var timeStampTypes = new[] { "time", "timestamp" }
        .ToDictionary(s => s, _ => EdmPrimitiveTypeKind.DateTimeOffset);

      map = map.Concat(stringTypes)
        .Concat(decimalTypes)
        .Concat(dateTimeTypes)
        .Concat(timeStampTypes)
        .ToDictionary(x => x.Key, x => x.Value);

      return map;
    }

    private void AddComponents(EdmModel model, EdmStructuredType entity, IEnumerable<ComponentInfo> components)
    {
      foreach (ComponentInfo componentInfo in components)
      {
        var edmSchemaElement = ((IEdmSchemaElement)entity);
        EdmComplexType complexType = new EdmComplexType(edmSchemaElement.Namespace, componentInfo.Name);

        foreach (var databaseColumn in componentInfo.Columns)
        {
          AddPropertyToEntity(complexType, databaseColumn);
        }

        //// add created type
        model.AddElement(complexType);

        if (componentInfo.ChildComponents != null && componentInfo.ChildComponents.Any())
        {
          //// we are passing here acutaly created complexType because it can be nested
          AddComponents(model, complexType, componentInfo.ChildComponents);
        }

        //// adding referenece to actual entity
        entity.AddStructuralProperty(componentInfo.Name.Remove(0, (edmSchemaElement.Name + _separator).Length), new EdmComplexTypeReference(complexType, false));
      }
    }

    private IEnumerable<ComponentInfo> BuildComponentMap(string entityName, string prefix, IEnumerable<DatabaseColumn> columns, int iteration = 0)
    {
      List<ComponentInfo> componentInfos  = new List<ComponentInfo>();

      var groupBy = columns.GroupBy(g => g.Name.Split(_separator)[iteration]);

      foreach (var g in groupBy)
      {
        var componentInfo = new ComponentInfo();
        componentInfo.Name = $"{entityName}{_separator}{prefix}{g.Key}";

        var actualGroupPrefix = $"{prefix}{g.Key}{_separator}";
        var actualLvlColumns = g
          .Select(s => new {Name = s.Name.Remove(0, (actualGroupPrefix).Length), Column = s})
          .ToList();

        componentInfo.Columns = actualLvlColumns.Where(w => !w.Name.Contains(_separator)).Select(s => s.Column);

        foreach (var databaseColumn in componentInfo.Columns)
        {
          databaseColumn.Name = databaseColumn.Name.Remove(0, actualGroupPrefix.Length);
        }

        var nextLvlComponentColumns = actualLvlColumns
          .Where(w => w.Name.Contains(_separator)).Select(s => s.Column)
          .ToList();

        if (nextLvlComponentColumns.Count > 0)
        {
          componentInfo.ChildComponents = BuildComponentMap(
            entityName,
            actualGroupPrefix,
            nextLvlComponentColumns,
            iteration + 1);
        }

        componentInfos.Add(componentInfo);
      }

      return componentInfos;
    }

    private class ComponentInfo
    {
      public IEnumerable<ComponentInfo> ChildComponents { get;  set; }

      public IEnumerable<DatabaseColumn> Columns { get;  set; }

      public string Name { get;  set; }
    }
  }
}