using System;

namespace DynamicOdata.Service.Owin
{
  public class ODataServiceSettings
  {
    internal ODataServiceSettings()
    {
      Services = new ODataServiceSettingsServices();
    }

    public string ConnectionString { get; set; }

    public string RoutePrefix { get; set; }

    public string Schema { get; set; }

    public ODataServiceSettingsServices Services { get; private set; }
  }

  public class ODataServiceSettingsServices
  {
    public Func<ODataServiceSettings, IDataService> DataService { get; set; }

    public Func<ODataServiceSettings, IEdmModelBuilder> EdmModelBuilder { get; set; }

    public Func<ODataServiceSettings, ISchemaReader> SchemaReader { get; set; }
  }
}