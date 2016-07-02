using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service
{
    public interface IDataService
    {
      int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);

      EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);

      EdmEntityObject Get(string key, IEdmEntityType entityType);
    }
}