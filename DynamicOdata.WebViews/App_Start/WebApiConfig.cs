using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using Autofac;
using Autofac.Integration.WebApi;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Impl.EdmBuilders;
using DynamicOdata.Service.Impl.ResultTransformers;
using DynamicOdata.Service.Impl.SchemaReaders;
using DynamicOdata.Service.Impl.SqlBuilders;
using DynamicOdata.WebViews.Infrastructure;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using Newtonsoft.Json;

namespace DynamicOdata.WebViews
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      RegisterAutofac(config);

      var routingConventions = ODataRoutingConventions.CreateDefault();
      routingConventions.Insert(0, new DynamicRoutingConvention());

      var oDataRoute = new ODataRoute(
        "odata",
        new CustomODataPathRouteConstraint(
          new DefaultODataPathHandler(),
          _ => (EdmModel)config.DependencyResolver.GetService(typeof(EdmModel)),
          "odata",
          routingConventions));

      config.Routes.Add("odata", oDataRoute);
      config.AddODataQueryFilter();
    }

    private static void RegisterAutofac(HttpConfiguration configuration)
    {
      var builder = new ContainerBuilder();
      builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

      var connectionStringSettings = ConfigurationManager.ConnectionStrings["default"];
      var schemaViewsReader = new SchemaViewsReader(connectionStringSettings.ConnectionString, "dbo");
      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(schemaViewsReader);
      var dataServiceV2 = new DataServiceV2(connectionStringSettings.ConnectionString, new SqlQueryBuilderWithObjectChierarchy('.'), new RowsToEdmObjectChierarchyResultTransformer('.'));

      builder.Register(_ => edmTreeModelBuilder.GetModel()).As<EdmModel>().SingleInstance();
      builder.Register(_ => dataServiceV2).As<IDataService>().SingleInstance();
      builder.Register(_ => schemaViewsReader).As<ISchemaReader>().SingleInstance();

      var container = builder.Build();

      var autofacResolver = new AutofacWebApiDependencyResolver(container);
      configuration.DependencyResolver = autofacResolver;
    }
  }
}