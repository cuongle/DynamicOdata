using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Impl.EdmBuilders;
using DynamicOdata.Service.Impl.ResultTransformers;
using DynamicOdata.Service.Impl.SchemaReaders;
using DynamicOdata.Service.Impl.SqlBuilders;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl
{
  [TestFixture]
  public class DataServiceTests
  {
    [Test]
    [Ignore("This is integration proof of concept test.")]
    public void Method_Scenario_Expected()
    {
      // Arrange

      string databaseConnStr = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|TestDatabase.mdf;Integrated Security=True";

      var dataService = new DataServiceV2(databaseConnStr, new SqlQueryBuilderWithObjectChierarchy('.'), new RowsToEdmObjectChierarchyResultTransformer('.') );
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(new SchemaViewsReader(databaseConnStr, "dbo"));
      var edmModel = edmTreeModelBuilder.GetModel();
      var edmSchemaType = edmModel.FindType("dbo.Case2Obligation");
      var edmCollectionType = edmModel.FindDeclaredEntityContainer("container").FindEntitySet("Case2Obligation");
      var oDataQueryContext = new ODataQueryContext(edmModel, edmSchemaType);
      HttpRequestMessage message = new HttpRequestMessage();
      message.RequestUri = new Uri("http://localhost:81/Case2Obligation?$orderby=Obligation/debtorName asc, Obligation/consumerIdentity/Number desc&$top=3&$skip=1");

      var oDataQueryOptions = new ODataQueryOptions(oDataQueryContext, message);
      oDataQueryOptions.Validate(new ODataValidationSettings());
      var collectionType =  (IEdmCollectionType)new EdmCollectionType((IEdmTypeReference)new EdmEntityTypeReference(edmCollectionType.ElementType, false));
      var edmEntityObjectCollection = dataService.Get(collectionType, oDataQueryOptions);

      // Act

      // Assert
    }
  }
}