using System;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using DynamicOdata.Service;
using Microsoft.Data.Edm;

namespace DynamicOdata.SelfHost
{
  internal class DataServiceExtensionWrapper : IDataService
  {
    private readonly IDataService _dataService;

    public DataServiceExtensionWrapper(IDataService dataService)
    {
      if (dataService == null)
      {
        throw new ArgumentNullException(nameof(dataService));
      }

      _dataService = dataService;
    }

    public int Count(IEdmCollectionType collectionType, ODataQueryOptions queryOptions)
    {
      Console.WriteLine("Counting....");

      return _dataService.Count(collectionType, queryOptions);
    }

    public EdmEntityObjectCollection Get(IEdmCollectionType collectionType, ODataQueryOptions queryOptions)
    {
      Console.WriteLine("Get list....");

      return _dataService.Get(collectionType, queryOptions);
    }

    public EdmEntityObject Get(string key, IEdmEntityType entityType)
    {
      Console.WriteLine("Get single entity....");

      return _dataService.Get(key, entityType);
    }
  }
}