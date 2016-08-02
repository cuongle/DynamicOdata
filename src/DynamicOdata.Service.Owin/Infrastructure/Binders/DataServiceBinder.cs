using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using DynamicOdata.Service.Impl;

namespace DynamicOdata.Service.Owin.Infrastructure.Binders
{
  internal class DataServiceBinder : IModelBinder
  {
    private readonly DataServiceV2 _dataServiceV2;

    public DataServiceBinder(DataServiceV2 dataServiceV2)
    {
      _dataServiceV2 = dataServiceV2;
    }

    public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
    {
      bindingContext.Model = _dataServiceV2;

      return true;
    }
  }
}