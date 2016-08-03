using System;

namespace DynamicOdata.Service.Owin
{
  public class ODataServiceSettings
  {
    public ODataServiceSettings()
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
    public Func<IDataService> DataService { get; set; }
  }
}