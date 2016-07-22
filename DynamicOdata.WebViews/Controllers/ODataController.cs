using System.Net;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using System.Web.ModelBinding;
using DynamicOdata.Service;
using Microsoft.Data.Edm;

namespace DynamicOdata.WebViews.Controllers
{
  public class OdataController : System.Web.Http.OData.ODataController
  {
    private readonly IDataService _dataService;

    public OdataController(IDataService dataService)
    {
      _dataService = dataService;
    }

    public EdmEntityObjectCollection Get([ModelBinder(typeof(ODataQueryOptionsBinder))] ODataQueryOptions queryOptions)
    {
      var oDataProperties = Request.ODataProperties();
      var collectionType = oDataProperties.Path.EdmType as IEdmCollectionType;

      // make $count works
      if (queryOptions.InlineCount != null)
      {
        oDataProperties.TotalCount = _dataService.Count(collectionType, queryOptions);
      }

      //make $select works
      if (queryOptions.SelectExpand != null)
      {
        oDataProperties.SelectExpandClause = queryOptions.SelectExpand.SelectExpandClause;
      }

      var collection = _dataService.Get(collectionType, queryOptions);

      return collection;
    }

    public IEdmEntityObject Get(string key)
    {
      ODataPath path = Request.ODataProperties().Path;
      IEdmEntityType entityType = path.EdmType as IEdmEntityType;

      var entity = _dataService.Get(key, entityType);

      // make sure return 404 if key does not exist in database
      if (entity == null)
      {
        throw new HttpResponseException(HttpStatusCode.NotFound);
      }

      return entity;
    }
  }
}