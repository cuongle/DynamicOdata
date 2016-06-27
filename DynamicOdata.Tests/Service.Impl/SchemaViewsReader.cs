using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Models;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl
{
  [TestFixture]
  public class SchemaViewsReaderTests
  {
    private string _dbConnectionString;
    private string _secondSchema = "specifiedSchema";

    [Test]
    public void GetTables_WhenNotPassedSchemaName_ThenViewsFromDefaultSchemaAreReturned()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString);

      // Act
      var databaseTables = schemaViewsReader.GetTables(null);

      // Assert
      Assert.AreEqual(2, databaseTables.Count());
    }

    [Test]
    public void GetTables_WhenPassedSchemaName_ThenViewsOnlyFromSpecifiedSchemaAreReturned()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString, _secondSchema);

      // Act
      var databaseTables = schemaViewsReader.GetTables(null);

      // Assert
      Assert.AreEqual(1, databaseTables.Count());
    }

    [Test]
    public void GetTables_WhenPassedViewFilter_ThenViewsOnlyFromSpecifiedFilterAreReturned()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString);

      // Act
      var databaseTables = schemaViewsReader.GetTables(new List<TableInfo> {new TableInfo
      {
        Name = "EntityView2",
        Schema = "dbo"
      } });

      // Assert
      Assert.AreEqual(1, databaseTables.Count());
    }

    [Test]
    public void GetTables_WhenPassedViewFilterAndSchema_ThenSchemaIsIgnored()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString, _secondSchema);

      // Act
      var databaseTables = schemaViewsReader.GetTables(new List<TableInfo> {new TableInfo
      {
        Name = "EntityView2",
        Schema = "dbo"
      } });

      // Assert
      Assert.AreEqual(1, databaseTables.Count());
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      _dbConnectionString = ConfigurationManager.ConnectionStrings["LocalDb"].ConnectionString;
    }
  }
}