using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  internal class CustomODataPathRouteConstraint : ODataPathRouteConstraint
  {
    private static readonly string _escapedSlash = Uri.HexEscape('/');
    private readonly IDataService _dataService;

    public CustomODataPathRouteConstraint(
      IODataPathHandler pathHandler,
      Func<HttpRequestMessage, IEdmModel> modelProvider,
      string routeName,
      IEnumerable<IODataRoutingConvention> routingConventions,
      IDataService dataService)
      : base(pathHandler, new EdmModel(), routeName, routingConventions)
    {
      _dataService = dataService;
      EdmModelProvider = modelProvider;
    }

    public Func<HttpRequestMessage, IEdmModel> EdmModelProvider { get; set; }

    public override bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
    {
      if (request == null)
      {
        throw new ArgumentNullException(nameof(request));
      }

      if (values == null)
      {
        throw new ArgumentNullException(nameof(values));
      }

      if (routeDirection != HttpRouteDirection.UriResolution)
      {
        return true;
      }

      object oDataPathValue;
      if (!values.TryGetValue(ODataRouteConstants.ODataPath, out oDataPathValue))
      {
        return false;
      }

      string oDataPathString = oDataPathValue as string;

      ODataPath path;
      IEdmModel model;
      try
      {
        model = EdmModelProvider(request);

        string requestLeftPart = request.RequestUri.GetLeftPart(UriPartial.Path);
        string serviceRoot = requestLeftPart;

        if (!string.IsNullOrEmpty(oDataPathString))
        {
          serviceRoot = RemoveODataPath(serviceRoot, oDataPathString);
        }

        string oDataPathAndQuery = requestLeftPart.Substring(serviceRoot.Length);
        oDataPathAndQuery = WebUtility.UrlDecode(oDataPathAndQuery);
        path = PathHandler.Parse(model, oDataPathAndQuery);
      }
      catch (Exception)
      {
        throw;
        //TODO: add logging
        throw new HttpResponseException(HttpStatusCode.NotFound);
      }

      if (path == null)
      {
        return false;
      }

      HttpRequestMessageProperties odataProperties = request.ODataProperties();
      odataProperties.Model = model;
      odataProperties.PathHandler = PathHandler;
      odataProperties.Path = path;
      odataProperties.RouteName = RouteName;
      odataProperties.RoutingConventions = RoutingConventions;
      values.Add(typeof(IDataService).FullName, _dataService);

      if (values.ContainsKey(ODataRouteConstants.Controller))
      {
        return true;
      }

      string controllerName = SelectControllerName(path, request);
      if (controllerName != null)
      {
        values[ODataRouteConstants.Controller] = controllerName;
      }

      return true;
    }

    private static string RemoveODataPath(string uriString, string oDataPathString)
    {
      int endIndex = uriString.Length - oDataPathString.Length - 1;
      if (endIndex <= 0)
      {
        throw new InvalidOperationException(
          $"Request Uri Is Too Short For ODataPath. the Uri is {uriString}, and the OData path is {oDataPathString}.");
      }

      string startString = uriString.Substring(0, endIndex + 1);  // Potential return value.
      string endString = uriString.Substring(endIndex + 1);       // Potential oDataPathString match.

      if (string.Equals(endString, oDataPathString, StringComparison.Ordinal))
      {
        return startString;
      }

      while (true)
      {
        int slashIndex = startString.LastIndexOf('/', endIndex - 1);
        int escapedSlashIndex = startString.LastIndexOf(_escapedSlash, endIndex - 1, StringComparison.OrdinalIgnoreCase);

        if (slashIndex > escapedSlashIndex)
        {
          endIndex = slashIndex;
        }
        else if (escapedSlashIndex >= 0)
        {
          endIndex = escapedSlashIndex + 1 + 1;
        }
        else
        {
          throw new InvalidOperationException($"The OData path is not found. The Uri is {uriString}, and the OData path is {oDataPathString}.");
        }

        startString = uriString.Substring(0, endIndex + 1);
        endString = uriString.Substring(endIndex + 1);

        endString = Uri.UnescapeDataString(endString);
        if (string.Equals(endString, oDataPathString, StringComparison.Ordinal))
        {
          return startString;
        }

        if (endIndex == 0)
        {
          throw new InvalidOperationException(
            $"The OData path is not found. The Uri is {uriString}, and the OData path is {oDataPathString}.");
        }
      }
    }
  }
}