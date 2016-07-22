using System.Linq;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  internal class SubstringOfFunction : IFunctionParser
  {
    public string FunctionName => "substringof";

    public string Parse(SingleValueFunctionCallNode node)
    {
      var property = node.Arguments.OfType<SingleValuePropertyAccessNode>().First();
      var value = node.Arguments.OfType<ConstantNode>().First();
      return string.Format("{0} like '%{1}%'", property.Property.Name, value.Value);
    }
  }
}