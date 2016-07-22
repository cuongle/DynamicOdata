using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData.Extensions;

namespace DynamicOdata.WebViews.Infrastructure.Binders
{
  internal class ODataRequestPropertiesBinder : IModelBinder
  {
    public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
    {
      bindingContext.Model = actionContext.Request.ODataProperties();

      return true;
    }
  }
}