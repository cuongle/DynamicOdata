using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl;
using Microsoft.Data.Edm;

namespace DynamicOdata.Web.Routing
{
    public static class DynamicModelHelper
    {
        public static ODataRoute CustomMapODataServiceRoute(HttpRouteCollection routes, string routeName, string routePrefix)
        {
            IList<IODataRoutingConvention> routingConventions = ODataRoutingConventions.CreateDefault();
            routingConventions.Insert(0, new DynamicRoutingConvention());

            return CustomMapODataServiceRoute(routes, routeName, routePrefix, GetModelFuncFromRequest(),
                new DefaultODataPathHandler(), routingConventions);
        }

        private static ODataRoute CustomMapODataServiceRoute(HttpRouteCollection routes, string routeName, string routePrefix,
            Func<HttpRequestMessage, IEdmModel> modelProvider, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions, ODataBatchHandler batchHandler = null)
        {
            if (!string.IsNullOrEmpty(routePrefix))
            {
                int prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }

            if (batchHandler != null)
            {
                batchHandler.ODataRouteName = routeName;

                string batchTemplate = string.IsNullOrEmpty(routePrefix)
                    ? ODataRouteConstants.Batch
                    : routePrefix + '/' + ODataRouteConstants.Batch;

                routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
            }

            var routeConstraint = new CustomODataPathRouteConstraint(pathHandler, modelProvider, routeName, routingConventions);
            var odataRoute = new CustomODataRoute(routePrefix, routeConstraint);

            routes.Add(routeName, odataRoute);

            return odataRoute;
        }

        private static Func<HttpRequestMessage, IEdmModel> GetModelFuncFromRequest()
        {
            return request =>
            {
                string odataPath = request.Properties[Constants.CustomODataPath] as string ?? string.Empty;
                string[] segments = odataPath.Split('/');
                string dataSource = segments[0];

                IEdmModelBuilder modelBuilder = new EdmModelBuilder(dataSource, new SchemaReader(dataSource));
                IEdmModel model = modelBuilder.GetModel();

                request.Properties[Constants.ODataDataSource] = dataSource;
                request.Properties[Constants.CustomODataPath] = string.Join("/", segments, 1, segments.Length - 1);

                return model;
            };
        }
    }
}