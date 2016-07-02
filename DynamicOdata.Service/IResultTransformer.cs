using System.Collections.Generic;
using System.Web.Http.OData;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service
{
  public interface IResultTransformer
  {
    EdmEntityObjectCollection Translate(IEnumerable<IDictionary<string, object>> fromRows, IEdmCollectionType toCollectionType);

    EdmEntityObject Translate(IDictionary<string, object> fromRow, IEdmType toType);
  }
}