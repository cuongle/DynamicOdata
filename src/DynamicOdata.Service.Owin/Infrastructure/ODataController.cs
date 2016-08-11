using System.Net;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using DynamicOdata.Service.Owin.Infrastructure.Binders;
using Microsoft.Data.Edm;
using Microsoft.Data.OData.Query;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  public class OdataController : System.Web.Http.OData.ODataController
  {
    public EdmEntityObjectCollection Get(
      [ModelBinder] ODataQueryOptions queryOptions,
      [ModelBinder] HttpRequestMessageProperties oDataProperties,
      [ModelBinder] IDataService dataService)
    {
      var collectionType = oDataProperties.Path.EdmType as IEdmCollectionType;

      // make $count works
      if (queryOptions.InlineCount != null)
      {
        oDataProperties.TotalCount = dataService.Count(collectionType, queryOptions);
      }

      //make $select works
      if (queryOptions.SelectExpand != null)
      {
        oDataProperties.SelectExpandClause = queryOptions.SelectExpand.SelectExpandClause;
      }

      var collection = dataService.Get(collectionType, queryOptions);

      return collection;
    }

    public IEdmEntityObject Get(string key, [ModelBinder]HttpRequestMessageProperties oDataProperties, [ModelBinder]IDataService dataService)
    {
      ODataPath path = oDataProperties.Path;
      IEdmEntityType entityType = path.EdmType as IEdmEntityType;

      var entity = dataService.Get(key, entityType);

      // make sure return 404 if key does not exist in database
      if (entity == null)
      {
        throw new HttpResponseException(HttpStatusCode.NotFound);
      }

      return entity;
    }
  }
}