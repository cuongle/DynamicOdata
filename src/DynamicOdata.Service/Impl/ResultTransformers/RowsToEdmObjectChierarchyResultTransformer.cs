using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Impl.ResultTransformers
{
  public class RowsToEdmObjectChierarchyResultTransformer : IResultTransformer
  {
    private static readonly ConcurrentDictionary<string, Dictionary<string, ComponentHelpClass>> EntityTypeToColumnsMap;
    private readonly char _separator;

    static RowsToEdmObjectChierarchyResultTransformer()
    {
      EntityTypeToColumnsMap = new ConcurrentDictionary<string, Dictionary<string, ComponentHelpClass>>();
    }

    public RowsToEdmObjectChierarchyResultTransformer(char treeSeparator)
    {
      if (treeSeparator <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(treeSeparator));
      }

      _separator = treeSeparator;
    }

    public EdmEntityObjectCollection Translate(IEnumerable<IDictionary<string, object>> fromRows, IEdmCollectionType toCollectionType)
    {
      var entityType = toCollectionType.ElementType.Definition as EdmEntityType;
      var collection = new EdmEntityObjectCollection(new EdmCollectionTypeReference(toCollectionType, true));

      var firstRow = fromRows.FirstOrDefault();

      if (firstRow == null)
      {
        return collection;
      }

      var componentHelpClasses = EntityTypeToColumnsMap.GetOrAdd(
        entityType.Name,
        entityName => BuildMap(firstRow));

      foreach (dynamic row in fromRows)
      {
        var entity = CreateEdmEntity(entityType, row, componentHelpClasses);
        collection.Add(entity);
      }

      return collection;
    }

    public EdmEntityObject Translate(IDictionary<string, object> fromRow, IEdmType toType)
    {
      if (fromRow == null)
      {
        return null;
      }

      var entityType = toType as EdmEntityType;

      var componentHelpClasses = EntityTypeToColumnsMap.GetOrAdd(
        entityType.Name,
        entityName => BuildMap(fromRow));

      return CreateEdmEntity(entityType, fromRow, componentHelpClasses);
    }

    private static void ParseComponent(EdmEntityObject entity, ComponentHelpClass item, object value)
    {
      int i = 0;

      EdmStructuredObject actualEntity = entity;
      for (i = 0; i < item.Depth - 1; i++)
      {
        var componentName = item.SplitedComponents[i];
        object local = null;
        actualEntity.TryGetPropertyValue(componentName, out local);
        if (local == null)
        {
          var declaredProperty = actualEntity.ActualEdmType.DeclaredProperties.FirstOrDefault(w => w.Name == componentName);
          var edmComplexObject = new EdmComplexObject(declaredProperty.Type.AsComplex());

          actualEntity.TrySetPropertyValue(componentName, edmComplexObject);
          actualEntity = edmComplexObject;
        }
        else
        {
          actualEntity = (EdmStructuredObject)local;
        }
      }

      var propertyName = item.SplitedComponents[item.Depth - 1];
      actualEntity.TrySetPropertyValue(propertyName, value);
    }

    private Dictionary<string, ComponentHelpClass> BuildMap(IDictionary<string, object> row)
    {
      if (row == null)
      {
        throw new ArgumentNullException(nameof(row));
      }

      Dictionary<string, ComponentHelpClass> dictionary = new Dictionary<string, ComponentHelpClass>();

      foreach (var column in row)
      {
        if (column.Key.Contains(_separator))
        {
          ComponentHelpClass expandoObject = new ComponentHelpClass();
          expandoObject.SplitedComponents = column.Key.Split(_separator);
          expandoObject.Depth = expandoObject.SplitedComponents.Length;

          dictionary.Add(column.Key, expandoObject);
        }
      }

      return dictionary;
    }

    private EdmEntityObject CreateEdmEntity(IEdmEntityType entityType, IDictionary<string, object> rows, Dictionary<string, ComponentHelpClass> columnMap)
    {
      if (rows == null)
      {
        return null;
      }

      var entity = new EdmEntityObject(entityType);

      foreach (var o in rows)
      {
        if (o.Key.Contains(_separator) == false)
        {
          entity.TrySetPropertyValue(o.Key, o.Value);
        }
        else
        {
          ParseComponent(entity, columnMap[o.Key], o.Value);
        }
      }

      return entity;
    }

    private class ComponentHelpClass
    {
      public int Depth { get; set; }

      public string[] SplitedComponents { get; set; }
    }
  }
}