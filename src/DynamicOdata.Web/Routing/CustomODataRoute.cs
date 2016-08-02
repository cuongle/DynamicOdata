using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Routing;
using System.Web.Http.Routing;

namespace DynamicOdata.Web.Routing
{
    public class CustomODataRoute : ODataRoute
    {
        private static readonly string EscapedHashMark = Uri.HexEscape('#');
        private static readonly string EscapedQuestionMark = Uri.HexEscape('?');

        private readonly bool _canGenerateDirectLink;

        public CustomODataRoute(string routePrefix, ODataPathRouteConstraint pathConstraint)
            : this(routePrefix, pathConstraint, defaults: null, constraints: null, dataTokens: null, handler: null)
        {
        }

        public CustomODataRoute(
            string routePrefix,
            ODataPathRouteConstraint pathConstraint,
            HttpRouteValueDictionary defaults,
            HttpRouteValueDictionary constraints,
            HttpRouteValueDictionary dataTokens,
            HttpMessageHandler handler)
            : base(routePrefix, pathConstraint, defaults, constraints, dataTokens, handler)
        {
            _canGenerateDirectLink = routePrefix != null && RoutePrefix.IndexOf('{') == -1;
        }

        public override IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request, IDictionary<string, object> values)
        {
            if (values == null || !values.Keys.Contains(HttpRoute.HttpRouteKey, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            object odataPathValue;
            if (!values.TryGetValue(ODataRouteConstants.ODataPath, out odataPathValue))
            {
                return null;
            }

            string odataPath = odataPathValue as string;
            if (odataPath != null)
            {
                return GenerateLinkDirectly(request, odataPath) ?? base.GetVirtualPath(request, values);
            }

            return null;
        }

        internal HttpVirtualPathData GenerateLinkDirectly(HttpRequestMessage request, string odataPath)
        {
            HttpConfiguration configuration = request.GetConfiguration();
            if (configuration == null || !_canGenerateDirectLink)
                return null;

            string odataEndpoint = request.Properties[Constants.ODataEndpoint] as string;
            string link = CombinePathSegments(RoutePrefix, odataEndpoint, odataPath);

            link = UriEncode(link);

            return new HttpVirtualPathData(this, link);
        }

        private static string CombinePathSegments(string routePrefix, string odataEndpoint, string odataPath)
        {
            string link = string.Empty;

            if (!string.IsNullOrEmpty(routePrefix))
                link += routePrefix + "/";

            if (!string.IsNullOrEmpty(odataEndpoint))
                link += odataEndpoint + "/";

            if (!string.IsNullOrEmpty(odataPath))
                link += odataPath;

            return link;
        }

        private static string UriEncode(string str)
        {
            string escape = Uri.EscapeUriString(str);
            escape = escape.Replace("#", EscapedHashMark);
            escape = escape.Replace("?", EscapedQuestionMark);
            return escape;
        }
    }
}