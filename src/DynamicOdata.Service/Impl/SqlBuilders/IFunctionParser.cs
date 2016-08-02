using Microsoft.Data.OData.Query;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  internal interface IFunctionParser
  {
    string FunctionName { get; }

    string Parse(SingleValueFunctionCallNode node);
  }
}