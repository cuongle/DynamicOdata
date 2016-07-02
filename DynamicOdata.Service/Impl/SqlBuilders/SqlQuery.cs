using System.Collections.Generic;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  public class SqlQuery
  {
    public SqlQuery()
    {
      Parameters = new Dictionary<string, object>();
    }

    public Dictionary<string, object> Parameters { get; set; }

    public string Query { get; set; }
  }
}