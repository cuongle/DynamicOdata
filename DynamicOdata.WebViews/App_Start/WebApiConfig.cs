using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using Autofac;
using Autofac.Integration.WebApi;
using DynamicOdata.Service;
using DynamicOdata.Service.Impl;
using DynamicOdata.Service.Impl.EdmBuilders;
using DynamicOdata.Service.Impl.SchemaReaders;
using Microsoft.Data.Edm;
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

      GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
      GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.None;
    }

    private static void RegisterAutofac(HttpConfiguration configuration)
    {
      var builder = new ContainerBuilder();
      builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

      var connectionStringSettings = ConfigurationManager.ConnectionStrings["default"];
      var schemaViewsReader = new SchemaViewsReader(connectionStringSettings.ConnectionString, "dbo");

      var edmTreeModelBuilder = new EdmObjectChierarchyModelBuilder(schemaViewsReader);

      builder.Register(_ => edmTreeModelBuilder.GetModel()).As<EdmModel>().SingleInstance();
      builder.Register(_ => new DataServiceV2("default")).As<IDataService>().SingleInstance();
      builder.Register(_ => schemaViewsReader).As<ISchemaReader>().SingleInstance();

      var container = builder.Build();

      var autofacResolver = new AutofacWebApiDependencyResolver(container);
      configuration.DependencyResolver = autofacResolver;
    }
  }

  public class DynamicRoutingConvention : IODataRoutingConvention
  {
    public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
    {
      return null;
    }

    public string SelectController(ODataPath odataPath, HttpRequestMessage request)
    {
      if (odataPath.Segments.FirstOrDefault() is EntitySetPathSegment)
        return "OData";

      return "DynamicOdataMetadata";
    }
  }

  public static class Constants
  {
    public const string CustomODataPath = "CustomODataPath";
    public const string ODataEndpoint = "ODataEndpoint";
  }

  public class CustomODataPathRouteConstraint : ODataPathRouteConstraint
  {
    private static readonly string EscapedSlash = Uri.HexEscape('/');

    public CustomODataPathRouteConstraint(
        IODataPathHandler pathHandler,
        Func<HttpRequestMessage, IEdmModel> modelProvider,
        string routeName,
        IEnumerable<IODataRoutingConvention> routingConventions)
        : base(pathHandler, new EdmModel(), routeName, routingConventions)
    {
      EdmModelProvider = modelProvider;
    }

    public Func<HttpRequestMessage, IEdmModel> EdmModelProvider { get; set; }

    public override bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
        IDictionary<string, object> values, HttpRouteDirection routeDirection)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (values == null)
        throw new ArgumentNullException(nameof(values));

      if (routeDirection != HttpRouteDirection.UriResolution)
        return true;

      object oDataPathValue;
      if (!values.TryGetValue(ODataRouteConstants.ODataPath, out oDataPathValue))
        return false;

      string oDataPathString = oDataPathValue as string;

      ODataPath path;
      IEdmModel model;
      try
      {
        request.Properties[Constants.CustomODataPath] = oDataPathString;

        model = EdmModelProvider(request);
        oDataPathString = (string)request.Properties[Constants.CustomODataPath];

        string requestLeftPart = request.RequestUri.GetLeftPart(UriPartial.Path);
        string serviceRoot = requestLeftPart;

        if (!string.IsNullOrEmpty(oDataPathString))
        {
          serviceRoot = RemoveODataPath(serviceRoot, oDataPathString);
        }

        string oDataPathAndQuery = requestLeftPart.Substring(serviceRoot.Length);
        path = PathHandler.Parse(model, oDataPathAndQuery);
      }
      catch (Exception ex)
      {
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
        throw new InvalidOperationException($"Request Uri Is Too Short For ODataPath. the Uri is {uriString}, and the OData path is {oDataPathString}.");

      string startString = uriString.Substring(0, endIndex + 1);  // Potential return value.
      string endString = uriString.Substring(endIndex + 1);       // Potential oDataPathString match.

      if (string.Equals(endString, oDataPathString, StringComparison.Ordinal))
        return startString;

      while (true)
      {
        int slashIndex = startString.LastIndexOf('/', endIndex - 1);
        int escapedSlashIndex = startString.LastIndexOf(EscapedSlash, endIndex - 1, StringComparison.OrdinalIgnoreCase);

        if (slashIndex > escapedSlashIndex)
        {
          endIndex = slashIndex;
        }
        else if (escapedSlashIndex >= 0)
        {
          endIndex = escapedSlashIndex + 2;
        }
        else
        {
          throw new InvalidOperationException($"The OData path is not found. The Uri is {uriString}, and the OData path is {oDataPathString}.");
        }

        startString = uriString.Substring(0, endIndex + 1);
        endString = uriString.Substring(endIndex + 1);

        endString = Uri.UnescapeDataString(endString);
        if (string.Equals(endString, oDataPathString, StringComparison.Ordinal))
          return startString;

        if (endIndex == 0)
          throw new InvalidOperationException($"The OData path is not found. The Uri is {uriString}, and the OData path is {oDataPathString}.");
      }
    }
  }
}