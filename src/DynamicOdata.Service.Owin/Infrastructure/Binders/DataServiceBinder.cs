using System;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using DynamicOdata.Service.Impl;

namespace DynamicOdata.Service.Owin.Infrastructure.Binders
{
  internal class DataServiceBinder : IModelBinder
  {
    public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
    {
      object dataService;
      actionContext.RequestContext.RouteData.Values.TryGetValue(typeof(IDataService).FullName, out dataService);
      bindingContext.Model = dataService;

      return true;
    }
  }
}