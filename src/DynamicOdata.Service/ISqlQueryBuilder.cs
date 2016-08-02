using System.Web.Http.OData.Query;
using DynamicOdata.Service.Impl.SqlBuilders;

namespace DynamicOdata.Service
{
  public interface ISqlQueryBuilder
  {
    SqlQuery ToCountSql(ODataQueryOptions queryOptions);

    SqlQuery ToSql(ODataQueryOptions queryOptions);
  }
}