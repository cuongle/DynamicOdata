using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using DynamicOdata.Service.Impl;
using Microsoft.Data.Edm;

namespace DynamicOdata.Web.Routing
{
    public static class DynamicModelHelper
    {
        public static ODataRoute CustomMapODataServiceRoute(HttpRouteCollection routes, string routeName, string routePrefix, HttpMessageHandler handler = null)
        {
            if (!string.IsNullOrEmpty(routePrefix))
            {
                int prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }

            var pathHandler = new DefaultODataPathHandler();

            var routingConventions = ODataRoutingConventions.CreateDefault();
            routingConventions.Insert(0, new DynamicRoutingConvention());

            var modelProvider = GetModelFuncFromRequest();

            var routeConstraint = new CustomODataPathRouteConstraint(pathHandler, modelProvider, routeName, routingConventions);

            var odataRoute = new CustomODataRoute(
                routePrefix: routePrefix,
                pathConstraint: routeConstraint,
                defaults: null,
                constraints: null,
                dataTokens: null,
                handler: handler);

            routes.Add(routeName, odataRoute);

            return odataRoute;
        }

        private static Func<HttpRequestMessage, IEdmModel> GetModelFuncFromRequest()
        {
            return request =>
            {
                string odataPath = request.Properties[Constants.CustomODataPath] as string ?? string.Empty;
                string[] segments = odataPath.Split('/');
                string odataEndpoint = segments[0];

                request.Properties[Constants.ODataEndpoint] = odataEndpoint;
                request.Properties[Constants.CustomODataPath] = string.Join("/", segments, 1, segments.Length - 1);

                var modelBuilder = new EdmModelBuilder(new SchemaReader(odataEndpoint));
                IEdmModel model = modelBuilder.GetModel();
                
                return model;
            };
        }
    }
}