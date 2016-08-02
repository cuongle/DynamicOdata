using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  internal class DynamicRoutingConvention : IODataRoutingConvention
  {
    private const string ControllerSuffix = "Controller";

    public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
    {
      return null;
    }

    public string SelectController(ODataPath odataPath, HttpRequestMessage request)
    {
      if (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment)
      {
        return typeof(OdataController).Name.Replace(ControllerSuffix, string.Empty);
      }

      return typeof(DynamicOdataMetadataController).Name.Replace(ControllerSuffix, string.Empty);
    }
  }
}