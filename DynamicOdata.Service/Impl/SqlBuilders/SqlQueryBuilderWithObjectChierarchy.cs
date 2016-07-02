using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  internal class SqlQueryBuilderWithObjectChierarchy : ISqlQueryBuilder
  {
    private readonly char _objectChierarchySeparator;

    public SqlQueryBuilderWithObjectChierarchy(char objectChierarchySeparator)
    {
      if (objectChierarchySeparator <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(objectChierarchySeparator));
      }

      _objectChierarchySeparator = objectChierarchySeparator;
    }

    public SqlQuery ToCountSql(ODataQueryOptions queryOptions)
    {
      var sqlQuery = new SqlQuery();
      var edmEntityType = queryOptions.Context.ElementType as EdmEntityType;

      sqlQuery.Query = FromClause(edmEntityType);
      sqlQuery.Query = $@"SELECT COUNT(*) FROM {sqlQuery.Query}";

      string whereClause = BuildWhereClause(queryOptions.Filter, sqlQuery.Parameters);
      if (!string.IsNullOrEmpty(whereClause))
      {
        sqlQuery.Query = $"{sqlQuery.Query} WHERE {whereClause}";
      }

      return sqlQuery;
    }

    public SqlQuery ToSql(ODataQueryOptions queryOptions)
    {
      var sqlQuery = new SqlQuery();
      var edmEntityType = queryOptions.Context.ElementType as EdmEntityType;
      sqlQuery.Query = FromClause(edmEntityType);
      string selectClause = BuildSelectClause(queryOptions.SelectExpand);

      sqlQuery.Query = $@"SELECT {selectClause} FROM {sqlQuery.Query}";

      Dictionary<string, object> parameters = new Dictionary<string, object>();

      string whereClause = BuildWhereClause(queryOptions.Filter, parameters);
      if (!string.IsNullOrEmpty(whereClause))
      {
        sqlQuery.Query = $"{sqlQuery.Query} WHERE {whereClause}";
      }

      string orderClause = BuildOrderClause(edmEntityType, queryOptions.OrderBy, queryOptions.Top, queryOptions.Skip);
      if (!string.IsNullOrEmpty(orderClause))
      {
        sqlQuery.Query = $"{sqlQuery.Query} ORDER BY {orderClause}";
      }

      return sqlQuery;
    }

    private static string BuildSelectClause(SelectExpandQueryOption selectExpandQueryOption)
    {
      if (selectExpandQueryOption == null)
        return "*"; // Select All

      return selectExpandQueryOption
        .RawSelect
        .Split(',')
        .Aggregate((a,b) => $"{a},[{b}]");
    }

    private string BuildFromPropertyNode(SingleValuePropertyAccessNode left, SingleValueNode right, BinaryOperatorKind operatorKind, Dictionary<string, object> parameters)
    {
      string result = string.Empty;
      if (right is ConstantNode)
      {
        result = BuildSingleClause(left, right as ConstantNode, operatorKind, parameters);
      }

      var rightSource = (right as ConvertNode)?.Source;
      if (rightSource is ConstantNode)
      {
        result = BuildSingleClause(left, rightSource as ConstantNode, operatorKind, parameters);
      }

      return result;
    }

    private string BuildOrderClause(EdmEntityType edmEntityType, OrderByQueryOption orderByQueryOption)
    {
      var orderClause = string.Empty;

      if (orderByQueryOption != null)
      {
        var columns = orderByQueryOption.RawValue.Split(',').AsEnumerable();
        var escapedColumns = columns.Select(
          s =>
          {
            var columnToOrder = s.Trim().Split(' ');
            string outputOrder = $"[{columnToOrder[0].Replace('/', _objectChierarchySeparator)}]";

            outputOrder += columnToOrder.Count() > 1 ? $" {columnToOrder[1]}" : string.Empty;

            return outputOrder;
          });
        orderClause = string.Join(",", escapedColumns);
      }
      else
      {
        var hasDeclareKey = HasDeclareKey(edmEntityType);
        if (hasDeclareKey)
        {
          var keys = edmEntityType.DeclaredKey;
          orderClause = string.Join(",", keys.Select(k => $"[{k.Name}]"));
        }
        else
        {
          var firstProperty = edmEntityType.DeclaredProperties.FirstOrDefault(w => w.Type.IsComplex() == false);
          if (firstProperty != null)
            orderClause = $"[{firstProperty.Name}]";
        }
      }

      return orderClause;
    }

    private string BuildOrderClause(EdmEntityType edmEntityType, OrderByQueryOption orderByQueryOption, TopQueryOption topQueryOption, SkipQueryOption skipQueryOption)
    {
      string orderClause = BuildOrderClause(edmEntityType, orderByQueryOption);
      string skipTopClause = BuildSkipTopClause(topQueryOption, skipQueryOption);

      if (string.IsNullOrEmpty(skipTopClause))
        return orderClause;

      return $"{orderClause} {skipTopClause}";
    }

    private string BuildSingleClause(SingleValuePropertyAccessNode propertyNode, ConstantNode valueNode, BinaryOperatorKind operatorKind, Dictionary<string, object> parameters)
    {
      string operatorString = string.Empty;

      switch (operatorKind)
      {
        case BinaryOperatorKind.Equal:
          operatorString = "=";
          break;

        case BinaryOperatorKind.NotEqual:
          operatorString = "!=";
          break;

        case BinaryOperatorKind.GreaterThan:
          operatorString = ">";
          break;

        case BinaryOperatorKind.GreaterThanOrEqual:
          operatorString = ">=";
          break;

        case BinaryOperatorKind.LessThan:
          operatorString = "<";
          break;

        case BinaryOperatorKind.LessThanOrEqual:
          operatorString = "<=";
          break;
      }

      if (valueNode.Value == null)
      {
        if (operatorKind != BinaryOperatorKind.Equal || operatorKind != BinaryOperatorKind.NotEqual)
        {
          throw new ArgumentException($"Value of property [{propertyNode.Property.Name}] is set to NULL value. It will not work...");
        }

        if (operatorKind == BinaryOperatorKind.Equal)
        {
          return $"([{propertyNode.Property.Name}] IS NULL)";
        }

        return $"([{propertyNode.Property.Name}] IS NOT NULL)";
      }

      //TODO: rewrite it to tree query naming

      parameters.Add(propertyNode.Property.Name, valueNode.Value);

      return $"([{propertyNode.Property.Name}] {operatorString} @{propertyNode.Property.Name})";
    }

    private string BuildSkipTopClause(TopQueryOption topQueryOption, SkipQueryOption skipQueryOption)
    {
      string skipTopClause = string.Empty;
      if (topQueryOption != null)
      {
        int skipValue = skipQueryOption?.Value ?? 0;
        skipTopClause = $"OFFSET {skipValue} ROWS FETCH NEXT {topQueryOption.Value} ROWS ONLY";
      }

      if (topQueryOption == null && skipQueryOption != null)
      {
        skipTopClause = $"OFFSET {skipQueryOption.Value} ROWS";
      }

      return skipTopClause;
    }

    private string BuildWhereClause(SingleValueNode node, Dictionary<string, object> parameters)
    {
      string result = string.Empty;

      var x = node as SingleValueFunctionCallNode;

      if (x != null)
      {
        switch (x.Name.ToLower())
        {
          case "substringof":
            var property = x.Arguments.OfType<SingleValuePropertyAccessNode>().First();
            var value = x.Arguments.OfType<ConstantNode>().First();
            result += string.Format("{0} like '%{1}%'", property.Property.Name, value.Value);
            break;

          case "startswith":
            var property1 = x.Arguments.OfType<SingleValuePropertyAccessNode>().First();
            var value2 = x.Arguments.OfType<ConstantNode>().First();
            result += string.Format("{0} like '{1}%'", property1.Property.Name, value2.Value);
            break;

          case "endswith":
            var property3 = x.Arguments.OfType<SingleValuePropertyAccessNode>().First();
            var value4 = x.Arguments.OfType<ConstantNode>().First();
            result += string.Format("{0} like '%{1}'", property3.Property.Name, value4.Value);
            break;
        }
      }

      var operatorNode = node as BinaryOperatorNode;
      if (operatorNode == null)
      {
        return result;
      }

      var left = operatorNode.Left;
      var right = operatorNode.Right;

      if (left is SingleValuePropertyAccessNode)
      {
        return BuildFromPropertyNode(left as SingleValuePropertyAccessNode, right, operatorNode.OperatorKind, parameters);
      }

      if (left is ConvertNode)
      {
        var leftSource = ((ConvertNode)left).Source;

        if (leftSource is SingleValuePropertyAccessNode)
        {
          return BuildFromPropertyNode(leftSource as SingleValuePropertyAccessNode, right, operatorNode.OperatorKind, parameters);
        }

        result += BuildWhereClause(leftSource, parameters);
      }

      if (left is BinaryOperatorNode)
      {
        result += BuildWhereClause(left, parameters);
      }

      result += " " + operatorNode.OperatorKind;

      if (right is BinaryOperatorNode)
      {
        result += " " + BuildWhereClause(right, parameters);
      }

      return result;
    }

    private string BuildWhereClause(FilterQueryOption filterQueryOption, Dictionary<string, object> parameters)
    {
      if (filterQueryOption == null)
      {
        return string.Empty;
      }

      var whereClause = BuildWhereClause(filterQueryOption.FilterClause.Expression, parameters);
      return whereClause;
    }

    private string FromClause(EdmEntityType edmEntityType)
    {
      return $"[{edmEntityType.Namespace}].[{edmEntityType.Name}]";
    }

    private bool HasDeclareKey(EdmEntityType edmEntityType)
    {
      if (edmEntityType.DeclaredKey == null)
      {
        return false;
      }

      return edmEntityType.DeclaredKey.Any();
    }
  }
}