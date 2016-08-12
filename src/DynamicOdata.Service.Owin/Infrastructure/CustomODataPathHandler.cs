using System.Web.Http.OData.Routing;
using Microsoft.Data.Edm;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  public class CustomODataPathHandler : DefaultODataPathHandler
  {
    protected override ODataPathSegment ParseAtEntityCollection(IEdmModel model, ODataPathSegment previous, IEdmType previousEdmType, string segment)
    {
      if (segment == CountPathSegment.SegmentKindConst)
      {
        return new CountPathSegment();
      }
      return base.ParseAtEntityCollection(model, previous, previousEdmType, segment);
    }
  }
}