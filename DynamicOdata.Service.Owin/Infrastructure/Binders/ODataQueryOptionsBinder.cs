using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service.Owin.Infrastructure.Binders
{
  internal class ODataQueryOptionsBinder : IModelBinder
  {
    public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
    {
      var oDataProperties = actionContext.Request.ODataProperties();
      ODataPath path = oDataProperties.Path;
      var collectionType = path.EdmType as IEdmCollectionType;
      var entityType = collectionType?.ElementType.Definition as IEdmEntityType;

      var queryContext = new ODataQueryContext(oDataProperties.Model, entityType);
      var queryOptions = new ODataQueryOptions(queryContext, actionContext.Request);

      bindingContext.Model = queryOptions;

      return true;
    }
  }
}