using System.Web.Http;
using System.Web.Http.OData.Extensions;
using Owin;

namespace DynamicOdata.Service.Owin
{
  public static class AppBuilderExtensions
  {
    public static void UseDynamicOData(this IAppBuilder app, HttpConfiguration config, ODataServiceSettings settings)
    {
      config.RegisterDynamicOData(settings);
    }
  }
}