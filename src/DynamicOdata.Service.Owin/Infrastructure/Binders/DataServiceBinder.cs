using System;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using DynamicOdata.Service.Impl;

namespace DynamicOdata.Service.Owin.Infrastructure.Binders
{
  internal class DataServiceBinder : IModelBinder
  {
    private readonly IDataService _dataService;

    public DataServiceBinder(IDataService dataService)
    {
      if (dataService == null)
      {
        throw new ArgumentNullException(nameof(dataService));
      }

      _dataService = dataService;
    }

    public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
    {
      bindingContext.Model = _dataService;

      return true;
    }
  }
}