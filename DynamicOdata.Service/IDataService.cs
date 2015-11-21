using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service
{
    public interface IDataService
    {
        EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);

        EdmEntityObject Get(string key, IEdmEntityType entityType);

        int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);
    }
}
