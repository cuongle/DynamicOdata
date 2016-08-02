using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DynamicOdata.Service.Impl.SchemaReaders;
using DynamicOdata.Service.Models;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl.SchemaReaders
{
  [TestFixture]
  public class SchemaViewsReaderTests
  {
    private string _dbConnectionString;
    private string _testSchema = "SchemaViewsReaderTests";

    [TestFixtureSetUp]
    public void OneTimeSetUp()
    {
      _dbConnectionString = ConfigurationManager.ConnectionStrings["LocalDb"].ConnectionString;
    }

    [Test]
    public void GetTables_PassedSchemaName_OnlyFromSpecifiedSchemaAreReturned()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString, _testSchema);

      // Act
      var databaseTables = schemaViewsReader.GetTables(null);

      // Assert
      Assert.AreEqual(1, databaseTables.Count());
    }

    [Test]
    public void GetTables_PassingViewFilter_IsNotSupported()
    {
      // Arrange

      var schemaViewsReader = new SchemaViewsReader(_dbConnectionString, "SchemaViewsReaderTests");

      // Act
      Assert.Throws<NotSupportedException>(() => schemaViewsReader.GetTables(new List<TableInfo> {new TableInfo
      {
        Name = "EntityView2",
        Schema = "dbo"
      } }));
    }
  }
}