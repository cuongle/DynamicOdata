using System.Net;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl;
using DynamicOdata.Web.Routing;
using Microsoft.Data.Edm;

namespace DynamicOdata.Web.Controllers
{
    public class DynamicController : ODataController
    {
        private IDataService GetDataService()
        {
            var sourceName = Request.Properties[Constants.ODataDataSource] as string;
            return new DataService(sourceName);
        }

        private IEdmModelBuilder GetEdmModelBuilder()
        {
            var sourceName = Request.Properties[Constants.ODataDataSource] as string;
            return new EdmModelBuilder(sourceName);
        }

        public EdmEntityObjectCollection Get()
        {
            ODataPath path = Request.ODataProperties().Path;
            var collectionType = path.EdmType as IEdmCollectionType;
            var entityType = collectionType.ElementType.Definition as IEdmEntityType;

            var dataProvider = GetDataService();
            var model = GetEdmModelBuilder().GetModel();

            var queryContext = new ODataQueryContext(model, entityType);
            var queryOptions = new ODataQueryOptions(queryContext, Request);

            // make $count works
            var oDataProperties = Request.ODataProperties();
            if (queryOptions.InlineCount != null)
            {
                oDataProperties.TotalCount = dataProvider.Count(collectionType, queryOptions);
            }

            //make $select works
            if (queryOptions.SelectExpand != null)
            {
                oDataProperties.SelectExpandClause = queryOptions.SelectExpand.SelectExpandClause;
            }

            var collection = dataProvider.Get(collectionType, queryOptions);

            return collection;
        }

        public IEdmEntityObject Get(string key)
        {
            ODataPath path = Request.ODataProperties().Path;
            IEdmEntityType entityType = path.EdmType as IEdmEntityType;

            var dataProvider = GetDataService();
            var entity = dataProvider.Get(key, entityType);

            // make sure return 404 if key does not exist in database
            if (entity == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            return entity;
        }

        public IEdmEntityObject Post(IEdmEntityObject entity)
        {
            var entityType = entity.GetEdmType().Definition as IEdmEntityType;

            var dataProvider = GetDataService();
            dataProvider.Insert(entityType, entity);

            return entity;
        }

        public IHttpActionResult Delete(string key)
        {
            ODataPath path = Request.ODataProperties().Path;
            IEdmEntityType entityType = path.EdmType as IEdmEntityType;

            var dataProvider = GetDataService();
            dataProvider.Delete(entityType, key);

            return StatusCode(HttpStatusCode.NoContent);
        }

        public IHttpActionResult Put(string key, IEdmEntityObject entity)
        {
            var entityType = entity.GetEdmType().Definition as IEdmEntityType;

            var dataProvider = GetDataService();
            dataProvider.Update(entityType, entity, key);

            return Updated(entity);
        }
    }
}
