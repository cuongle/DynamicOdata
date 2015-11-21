using System;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.OData.Extensions;
using Autofac;
using Autofac.Integration.WebApi;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl;
using DynamicOdata.Web.Routing;

namespace DynamicOdata.Web
{
    public static class WebApiConfig
    {
        private static void RegisterAutofac(HttpConfiguration configuration)
        {
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            Func<string> odataEndpointFunc = () =>
            {
                HttpRequestMessage httpRequestMessage =
                    HttpContext.Current.Items["MS_HttpRequestMessage"] as HttpRequestMessage;
                var odataEndpoint = httpRequestMessage?.Properties["ODataEndpoint"] as string;

                return odataEndpoint;
            };

            builder.Register(_ => new DataService(odataEndpointFunc())).As<IDataService>();
            builder.Register(_ => new SchemaReader(odataEndpointFunc())).As<ISchemaReader>();
         
            builder.RegisterType<EdmModelBuilder>().As<IEdmModelBuilder>();

            var container = builder.Build();

            var autofacResolver = new AutofacWebApiDependencyResolver(container);
            configuration.DependencyResolver = autofacResolver;
        }

        public static void Register(HttpConfiguration config)
        {
            RegisterAutofac(config);

            DynamicModelHelper.CustomMapODataServiceRoute(config.Routes, "odata", "odata");
            config.AddODataQueryFilter();
        }
    }
}