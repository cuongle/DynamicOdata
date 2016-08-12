using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.OData.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  public class CountPathSegment : ODataPathSegment
  {
    public const string SegmentKindConst = "$count";

    public override string SegmentKind => SegmentKindConst;

    public override IEdmType GetEdmType(IEdmType previousEdmType)
    {
      return EdmCoreModel.Instance.FindDeclaredType("Edm.Int32");
    }

    public override IEdmEntitySet GetEntitySet(IEdmEntitySet previousEntitySet)
    {
      return previousEntitySet;
    }

    public override string ToString()
    {
      return SegmentKindConst;
    }
  }
}