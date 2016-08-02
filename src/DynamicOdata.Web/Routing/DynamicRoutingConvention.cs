using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace DynamicOdata.Web.Routing
{
    public class DynamicRoutingConvention : IODataRoutingConvention
    {
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            return null;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment)
                return "Dynamic";

            return "DynamicOdataMetadata";
        }
    }
}