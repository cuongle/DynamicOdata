using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  public class SqlQueryBuilderWithObjectHierarchy : ISqlQueryBuilder
  {
    private static readonly Dictionary<string, IFunctionParser> FunctionsParsers;
    private static ODataValidationSettings _supportedODataQueryOptions;
    private readonly char _objectHierarchySeparator;

    static SqlQueryBuilderWithObjectHierarchy()
    {
      FunctionsParsers = new Dictionary<string, IFunctionParser>();

      InitializeFunctionParsers(
        new SubstringOfFunction(),
        new StartsWithFunction(),
        new EndsWithFunction());

      _supportedODataQueryOptions = new ODataValidationSettings();

      _supportedODataQueryOptions.AllowedArithmeticOperators = AllowedArithmeticOperators.None;
      _supportedODataQueryOptions.AllowedFunctions = AllowedFunctions.StartsWith
                                                 | AllowedFunctions.EndsWith
                                                 | AllowedFunctions.Substring;
      _supportedODataQueryOptions.AllowedLogicalOperators = AllowedLogicalOperators.All;
      _supportedODataQueryOptions.AllowedQueryOptions = AllowedQueryOptions.Filter
                                                    | AllowedQueryOptions.InlineCount
                                                    | AllowedQueryOptions.OrderBy
                                                    | AllowedQueryOptions.Skip
                                                    | AllowedQueryOptions.Top;

      SetMaxNodeCountFromConfigFile();
    }

    private static void SetMaxNodeCountFromConfigFile()
    {
      string customMaxNodeCount = ConfigurationManager.AppSettings["DynamicOData.ODataValidation.MaxNodeCount"];
      if (string.IsNullOrEmpty(customMaxNodeCount) == false)
      {
        int maxNodeCountValue;
        if (int.TryParse(customMaxNodeCount, out maxNodeCountValue) && maxNodeCountValue >= 1)
        {
          _supportedODataQueryOptions.MaxNodeCount = maxNodeCountValue;
        }
      }
    }

    public SqlQueryBuilderWithObjectHierarchy(char objectHierarchySeparator)
    {
      if (objectHierarchySeparator <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(objectHierarchySeparator));
      }

      _objectHierarchySeparator = objectHierarchySeparator;
    }

    public static ODataValidationSettings GetSupportedODataQueryOptions()
    {
      return _supportedODataQueryOptions;
    }

    public SqlQuery ToCountSql(ODataQueryOptions queryOptions)
    {
      ValidateQuery(queryOptions);

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
      ValidateQuery(queryOptions);

      var sqlQuery = new SqlQuery();
      var edmEntityType = queryOptions.Context.ElementType as EdmEntityType;
      sqlQuery.Query = FromClause(edmEntityType);

      sqlQuery.Query = $@"SELECT * FROM {sqlQuery.Query}";
      string whereClause = BuildWhereClause(queryOptions.Filter, sqlQuery.Parameters);

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

    private static void InitializeFunctionParsers(params IFunctionParser[] functionParsers)
    {
      foreach (var functionParser in functionParsers)
      {
        FunctionsParsers.Add(functionParser.FunctionName, functionParser);
      }
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
            string outputOrder = $"[{columnToOrder[0].Replace('/', _objectHierarchySeparator)}]";

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

    private string BuildSingleClause(
      SingleValuePropertyAccessNode propertyNode,
      ConstantNode valueNode,
      BinaryOperatorKind operatorKind,
      Dictionary<string, object> parameters)
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
        if (operatorKind != BinaryOperatorKind.Equal && operatorKind != BinaryOperatorKind.NotEqual)
        {
          throw new ArgumentException($"Value of property [{propertyNode.Property.Name}] is set to NULL value. It will not work...");
        }

        if (operatorKind == BinaryOperatorKind.Equal)
        {
          return $"([{propertyNode.Property.Name}] IS NULL)";
        }

        return $"([{propertyNode.Property.Name}] IS NOT NULL)";
      }

      List<string> paths = new List<string>();

      do
      {
        paths.Add(propertyNode.Property.Name);

        propertyNode = propertyNode.Source as SingleValuePropertyAccessNode;
      } while (propertyNode != null);

      paths.Reverse();

      var columnName = string.Join(_objectHierarchySeparator.ToString(), paths);

      var parameterName = string.Join("_", paths);

      var parameterAlreadyAddedCount = parameters.Keys.Count(w => w.StartsWith(parameterName, StringComparison.InvariantCultureIgnoreCase));

      parameterName = $"{parameterName}_{parameterAlreadyAddedCount}";

      parameters.Add(parameterName, valueNode.Value);

      return $"([{columnName}] {operatorString} @{parameterName})";
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
        var nodeName = x.Name.ToLower();

        if (FunctionsParsers.ContainsKey(nodeName))
        {
          result += FunctionsParsers[nodeName].Parse(x);
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
        result += " (" + BuildWhereClause(left, parameters) + ") ";
      }

      result += " " + operatorNode.OperatorKind;

      if (right is BinaryOperatorNode)
      {
        result += " (" + BuildWhereClause(right, parameters) + ") ";
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

    private void ValidateQuery(ODataQueryOptions queryOptions)
    {
      queryOptions.Validate(GetSupportedODataQueryOptions());
    }
  }
}