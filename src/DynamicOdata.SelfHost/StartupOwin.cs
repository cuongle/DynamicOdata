using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Http.OData.Extensions;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Impl.ResultTransformers;
using DynamicOdata.Service.Impl.SqlBuilders;
using DynamicOdata.Service.Owin;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DynamicOdata.SelfHost.StartupOwin))]

namespace DynamicOdata.SelfHost
{
  public class StartupOwin
  {
    public void Configuration(IAppBuilder app)
    {
      var oDataServiceSettings = new ODataServiceSettings();
      oDataServiceSettings.ConnectionString = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
      oDataServiceSettings.RoutePrefix = "odata";
      oDataServiceSettings.Schema = "dbo";

      //// extension point for replacing data service - by Default instance does not need to be set up
      ////
      ////
      ////oDataServiceSettings.Services.DataService = () => new DataServiceExtensionWrapper(
      ////  new DataServiceV2(oDataServiceSettings.ConnectionString,
      ////  new SqlQueryBuilderWithObjectChierarchy('.'),
      ////  new RowsToEdmObjectChierarchyResultTransformer('.')));

      HttpConfiguration config = new HttpConfiguration();

      config.EnableSystemDiagnosticsTracing();

      app.UseDynamicOData(config, oDataServiceSettings);
      app.UseWebApi(config);
    }
  }
}