using System.Web.Http;
using System.Web.Http.OData.Extensions;
using DynamicOdata.Web.Routing;

namespace DynamicOdata.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            DynamicModelHelper.CustomMapODataServiceRoute(config.Routes, "odata", "odata");
            config.AddODataQueryFilter();
        }
    }
}