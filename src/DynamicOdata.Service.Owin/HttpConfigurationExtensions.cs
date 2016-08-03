using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ModelBinding;
using System.Web.Http.ModelBinding.Binders;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Impl.EdmBuilders;
using DynamicOdata.Service.Impl.ResultTransformers;
using DynamicOdata.Service.Impl.SchemaReaders;
using DynamicOdata.Service.Impl.SqlBuilders;
using DynamicOdata.Service.Owin.Infrastructure;
using DynamicOdata.Service.Owin.Infrastructure.Binders;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Owin
{
  public static class HttpConfigurationExtensions
  {
    public static void RegisterDynamicOData(this HttpConfiguration config, Action<ODataServiceSettings> configureSettings)
    {
      var routeName = $"ODataService_{Guid.NewGuid().ToString("N")}";

      var routingConventions = ODataRoutingConventions.CreateDefault();
      routingConventions.Insert(0, new DynamicRoutingConvention());

      ODataServiceSettings settings = new ODataServiceSettings();
      settings.Services.DataService = s => new DataServiceV2(
          s.ConnectionString,
          new SqlQueryBuilderWithObjectChierarchy('.'),
          new RowsToEdmObjectChierarchyResultTransformer('.'));

      configureSettings(settings);

      var schemaViewsReader = new SchemaViewsReader(settings.ConnectionString, settings.Schema);
      var edmModel = new EdmObjectChierarchyModelBuilder(schemaViewsReader).GetModel();

      var oDataRoute = new ODataRoute(
        settings.RoutePrefix,
        new CustomODataPathRouteConstraint(
          new DefaultODataPathHandler(),
          _ => edmModel,
          routeName,
          routingConventions));

      IDataService dataService = settings.Services.DataService(settings);

      config.Services.Insert(typeof(ModelBinderProvider), 0, new SimpleModelBinderProvider(typeof(ODataQueryOptions), new ODataQueryOptionsBinder()));
      config.Services.Insert(typeof(ModelBinderProvider),0,new SimpleModelBinderProvider(typeof(HttpRequestMessageProperties), new ODataRequestPropertiesBinder()));
      config.Services.Insert(typeof(ModelBinderProvider), 0, new SimpleModelBinderProvider(typeof(IDataService), () => new DataServiceBinder(dataService)));

      config.Routes.Add(routeName, oDataRoute);
    }
  }
}