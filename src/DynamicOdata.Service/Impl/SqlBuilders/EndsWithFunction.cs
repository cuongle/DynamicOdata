using System.Linq;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  internal class EndsWithFunction : IFunctionParser
  {
    public string FunctionName => "endswith";

    public string Parse(SingleValueFunctionCallNode node)
    {
      var property1 = node.Arguments.OfType<SingleValuePropertyAccessNode>().First();
      var value2 = node.Arguments.OfType<ConstantNode>().First();
      return string.Format("{0} like '%{1}'", property1.Property.Name, value2.Value);
    }
  }
}