using DynamicOdata.Service.Impl.SqlBuilders;
using System;
using System.Web.Http.OData.Query;

namespace DynamicOdata.Service.Owin
{
  public class ODataServiceSettings
  {
    internal ODataServiceSettings()
    {
      ValidationSettings = SupportedODataQueryOptions.GetDefaultDataServiceV2();

      Services = new ODataServiceSettingsServices();
    }

    public string ConnectionString { get; set; }

    public string RoutePrefix { get; set; }

    public string Schema { get; set; }

    public ODataValidationSettings ValidationSettings { get; }

    public ODataServiceSettingsServices Services { get; }
  }

  public class ODataServiceSettingsServices
  {
    public Func<ODataServiceSettings, IDataService> DataService { get; set; }

    public Func<ODataServiceSettings, IEdmModelBuilder> EdmModelBuilder { get; set; }

    public Func<ODataServiceSettings, ISchemaReader> SchemaReader { get; set; }
  }
}