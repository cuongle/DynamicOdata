using System.Configuration;
using System.Web.Http;
using DynamicOdata.Service.Owin;

namespace DynamicOdata.WebViews
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      config.RegisterDynamicOData(
        oDataServiceSettings =>
        {
          oDataServiceSettings.ConnectionString = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
          oDataServiceSettings.RoutePrefix = "odata";
          oDataServiceSettings.Schema = "dbo";
        });
    }
  }
}