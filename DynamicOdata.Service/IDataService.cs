using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service
{
    public interface IDataService
    {
        EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);

        EdmEntityObject Get(string key, IEdmEntityType entityType);

        void Insert(IEdmEntityType entityType, IEdmEntityObject entity);

        void Update(IEdmEntityType entityType, IEdmEntityObject entity, string key);

        void Delete(IEdmEntityType entityType, string key);

        int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions);
    }
}
