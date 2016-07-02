using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl.EdmBuilders;
using DynamicOdata.Service.Models;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.Edm.Validation;
using Moq;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl.EdmBuilders
{
  [TestFixture]
  public class EdmTreeModelBuilderTests
  {
    [Test]
    public void GetModel_DottedChierarchyColumns_ShouldBeNotBeMixedWithOtherTableComponents()
    {
      // Arrange
      var mock = new Mock<ISchemaReader>();
      var tableName = "x";
      var c1Name = "Obligation";
      var c2Name = "Subject";
      var c3Name = "IdentityNumber";
      var c3Property1Name = "Value";
      var c3Property2Name = "Type";

      var firstColumn = new DatabaseColumn { Name = $"{c1Name}.{c2Name}.{c3Name}.{c3Property1Name}", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };
      var secondColumn = new DatabaseColumn { Name = $"{c1Name}.{c2Name}.{c3Name}.{c3Property2Name}", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };

      DatabaseTable[] tables = {
        new DatabaseTable()
        {
          Name = tableName,
          Schema = "dbo",
          Columns = new[]
          {
            firstColumn,
            secondColumn
          }
        }
      };

      mock.Setup(s => s.GetTables(It.IsAny<IEnumerable<TableInfo>>())).Returns(tables);

      // Act
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(mock.Object);
      var edmModel = edmTreeModelBuilder.GetModel();

      // Assert
      var edmSchemaElement = edmModel.SchemaElements.FirstOrDefault(f => f.Name == tableName);
      Assert.NotNull(edmSchemaElement);

      var edmEntityType = edmSchemaElement as EdmEntityType;

      var component1 = edmEntityType.DeclaredProperties.First(f => f.Name == c1Name) as EdmStructuralProperty;
      Assert.NotNull(component1);
      EdmComplexType edmComplexType = ((EdmComplexType)component1.Type.Definition);
      Assert.IsTrue(edmComplexType.Name.StartsWith(tableName));

      var component2 = edmComplexType.DeclaredProperties.First(f => f.Name == c2Name) as EdmStructuralProperty;
      Assert.NotNull(component2);
      edmComplexType = ((EdmComplexType)component2.Type.Definition);
      Assert.IsTrue(edmComplexType.Name.StartsWith(tableName));

      var component3 = edmComplexType.DeclaredProperties.First(f => f.Name == c3Name) as EdmStructuralProperty;
      Assert.NotNull(component3);
      edmComplexType = ((EdmComplexType)component3.Type.Definition);
      Assert.IsTrue(edmComplexType.Name.StartsWith(tableName));
    }

    [Test]
    public void GetModel_PrimaryKeyColumnInDatabaseTable_IsSetOnEdmType()
    {
      // Arrange
      var mock = new Mock<ISchemaReader>();
      var tableName = "x";
      var pkColumn = new DatabaseColumn { Name = "Id", IsPrimaryKey = true, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };
      var firstColumn = new DatabaseColumn { Name = "FirstName", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };

      DatabaseTable[] tables = {
        new DatabaseTable()
        {
          Name = tableName,
          Schema = "dbo",
          Columns = new[]
          {
            pkColumn,
            firstColumn
          }
        }
      };

      mock.Setup(s => s.GetTables(It.IsAny<IEnumerable<TableInfo>>())).Returns(tables);

      // Act
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(mock.Object);
      var edmModel = edmTreeModelBuilder.GetModel();

      // Assert
      var edmSchemaElement = edmModel.SchemaElements.FirstOrDefault(f => f.Name == tableName);
      Assert.NotNull(edmSchemaElement);

      var edmEntityType = edmSchemaElement as EdmEntityType;

      var pkProperty = edmEntityType.DeclaredKey.FirstOrDefault(f => f.Name == pkColumn.Name);
      Assert.NotNull(pkProperty);
    }

    [Test]
    public void GetModel_PrimaryKeyColumnNotExistsInDatabaseTable_IsNotSetOnEdmType()
    {
      // Arrange
      var mock = new Mock<ISchemaReader>();
      var tableName = "x";
      var firstColumn = new DatabaseColumn { Name = "FirstName", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };

      DatabaseTable[] tables = {
        new DatabaseTable()
        {
          Name = tableName,
          Schema = "dbo",
          Columns = new[]
          {
            firstColumn
          }
        }
      };

      mock.Setup(s => s.GetTables(It.IsAny<IEnumerable<TableInfo>>())).Returns(tables);

      // Act
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(mock.Object);
      var edmModel = edmTreeModelBuilder.GetModel();

      // Assert
      var edmSchemaElement = edmModel.SchemaElements.FirstOrDefault(f => f.Name == tableName);
      Assert.NotNull(edmSchemaElement);

      var edmEntityType = edmSchemaElement as EdmEntityType;

      Assert.IsNull(edmEntityType.DeclaredKey);
    }

    [Test]
    public void GetModel_SingleTableWithDottedChierarchy_EdmWithNestedComplexTypesIsGenerated()
    {
      // Arrange
      var mock = new Mock<ISchemaReader>();
      var tableName = "x";
      var c1Name = "Obligation";
      var c2Name = "Subject";
      var c3Name = "IdentityNumber";
      var c3Property1Name = "Value";
      var c3Property2Name = "Type";

      var firstColumn = new DatabaseColumn { Name = $"{c1Name}.{c2Name}.{c3Name}.{c3Property1Name}", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };
      var secondColumn = new DatabaseColumn { Name = $"{c1Name}.{c2Name}.{c3Name}.{c3Property2Name}", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo" };

      DatabaseTable[] tables = {
        new DatabaseTable()
        {
          Name = tableName,
          Schema = "dbo",
          Columns = new[]
          {
            firstColumn,
            secondColumn
          }
        }
      };

      mock.Setup(s => s.GetTables(It.IsAny<IEnumerable<TableInfo>>())).Returns(tables);

      // Act
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(mock.Object);
      var edmModel = edmTreeModelBuilder.GetModel();

      // Assert
      var edmSchemaElement = edmModel.SchemaElements.FirstOrDefault(f => f.Name == tableName);
      Assert.NotNull(edmSchemaElement);

      var edmEntityType = edmSchemaElement as EdmEntityType;

      var component1 = edmEntityType.DeclaredProperties.First(f => f.Name == c1Name) as EdmStructuralProperty;
      Assert.NotNull(component1);
      var component2 = ((EdmComplexType)component1.Type.Definition).DeclaredProperties.First(f => f.Name == c2Name) as EdmStructuralProperty;
      Assert.NotNull(component2);
      var component3 = ((EdmComplexType)component2.Type.Definition).DeclaredProperties.First(f => f.Name == c3Name) as EdmStructuralProperty;
      Assert.NotNull(component3);

      var edmComponent3 = (EdmComplexType)component3.Type.Definition;

      Assert.AreEqual(tables[0].Columns.Count(), edmComponent3.DeclaredProperties.Count());

      Assert.IsTrue(edmComponent3.DeclaredProperties.Any(a => a.Name == c3Property1Name));
      Assert.IsTrue(edmComponent3.DeclaredProperties.Any(a => a.Name == c3Property2Name));
    }

    [Test]
    public void GetModel_TableInDatabaseSchema_SameTypeIsPlacedInSchemaElements()
    {
      // Arrange
      var mock = new Mock<ISchemaReader>();
      var tableName = "x";
      var secondColumn = new DatabaseColumn {Name = "SecondName", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo"};
      var firstColumn = new DatabaseColumn {Name = "FirstName", IsPrimaryKey = false, DataType = "nvarchar", Nullable = false, Table = tableName, Schema = "dbo"};

      DatabaseTable[] tables = {
        new DatabaseTable()
        {
          Name = tableName,
          Schema = "dbo",
          Columns = new[]
          {
            firstColumn,
            secondColumn
          }
        }
      };

      mock.Setup(s => s.GetTables(It.IsAny<IEnumerable<TableInfo>>())).Returns(tables);

      // Act
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(mock.Object);
      var edmModel = edmTreeModelBuilder.GetModel();

      // Assert
      var edmSchemaElement = edmModel.SchemaElements.FirstOrDefault(f => f.Name == tableName);
      Assert.NotNull(edmSchemaElement);

      var edmEntityType = edmSchemaElement as EdmEntityType;

      var firstColumnDesclaredProperty = edmEntityType.DeclaredProperties.FirstOrDefault(f => f.Name == firstColumn.Name);
      Assert.NotNull(firstColumnDesclaredProperty);

      var secondColumnDesclaredProperty = edmEntityType.DeclaredProperties.FirstOrDefault(f => f.Name == secondColumn.Name);
      Assert.NotNull(secondColumnDesclaredProperty);
    }
  }
}