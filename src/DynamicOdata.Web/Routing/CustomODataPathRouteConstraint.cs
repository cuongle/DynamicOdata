using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Web.Routing
{
    public class CustomODataPathRouteConstraint : ODataPathRouteConstraint
    {
        private static readonly string EscapedSlash = Uri.HexEscape('/');

        public Func<HttpRequestMessage, IEdmModel> EdmModelProvider { get; set; }

        public CustomODataPathRouteConstraint(
            IODataPathHandler pathHandler,
            Func<HttpRequestMessage, IEdmModel> modelProvider,
            string routeName,
            IEnumerable<IODataRoutingConvention> routingConventions)
            : base(pathHandler, new EdmModel(), routeName, routingConventions)
        {
            EdmModelProvider = modelProvider;
        }

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