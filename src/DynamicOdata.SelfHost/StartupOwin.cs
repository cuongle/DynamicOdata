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
      HttpConfiguration config = new HttpConfiguration();

      config.EnableSystemDiagnosticsTracing();

      app.UseDynamicOData(
        config,
        oDataServiceSettings =>
        {
          oDataServiceSettings.ConnectionString = ConfigurationManager.ConnectionStrings["first"].ConnectionString;
          oDataServiceSettings.RoutePrefix = "odata";
          oDataServiceSettings.Schema = "dbo";

          //// extension point for replacing data service - by Default instance does not need to be set up
          ////
          //// oDataServiceSettings.Services.DataService = s => new DataServiceDecorator(s.Services.DataService(s));
        });

      app.UseDynamicOData(
        config,
        oDataServiceSettings =>
        {
          oDataServiceSettings.ConnectionString = ConfigurationManager.ConnectionStrings["second"].ConnectionString;
          oDataServiceSettings.RoutePrefix = "odata2";
          oDataServiceSettings.Schema = "dbo";

          //// extension point for replacing data service - by Default instance does not need to be set up
          ////
          //// oDataServiceSettings.Services.DataService = s => new DataServiceDecorator(s.Services.DataService(s));
        });

      app.UseWebApi(config);
    }
  }
}