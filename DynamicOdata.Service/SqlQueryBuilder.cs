using System.Linq;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace DynamicOdata.Service
{
    public class SqlQueryBuilder
    {
        private readonly ODataQueryOptions _queryOptions;
        private readonly EdmEntityType _edmEntityType;

        public SqlQueryBuilder(ODataQueryOptions queryOptions)
        {
            _queryOptions = queryOptions;
            _edmEntityType = queryOptions.Context.ElementType as EdmEntityType;
        }

        private static string BuildSelectClause(SelectExpandQueryOption selectExpandQueryOption)
        {
            if (selectExpandQueryOption == null)
                return "*"; // Select All

            var columns = selectExpandQueryOption.RawSelect.Split(',').AsEnumerable();

            string selectClause = string.Join(",", columns);

            return selectClause;
        }

        private string FromClause()
        {
            return $"[{_edmEntityType.Namespace}].[{_edmEntityType.Name}]";
        }

        private string BuildSingleClause(SingleValuePropertyAccessNode propertyNode, ConstantNode valueNode, BinaryOperatorKind operatorKind)
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

            string valueString = valueNode.Value?.ToString() ?? "null";

            if (valueNode.Value == null)
            {
                if (operatorKind == BinaryOperatorKind.Equal)
                    return $"({propertyNode.Property.Name} IS NULL)";

                if (operatorKind == BinaryOperatorKind.NotEqual)
                    return $"({propertyNode.Property.Name} IS NOT NULL)";
            }

            return $"({propertyNode.Property.Name} {operatorString} {valueString})";
        }

        private string BuildFromPropertyNode(SingleValuePropertyAccessNode left, SingleValueNode right, BinaryOperatorKind operatorKind)
        {
            string result = string.Empty;
            if (right is ConstantNode)
            {
                result = BuildSingleClause(left, right as ConstantNode, operatorKind);
            }

            var rightSource = (right as ConvertNode)?.Source;
            if (rightSource is ConstantNode)
            {
                result = BuildSingleClause(left, rightSource as ConstantNode, operatorKind);
            }

            return result;
        }

        private string BuildWhereClause(SingleValueNode node)
        {
            string result = string.Empty;

            var operatorNode = node as BinaryOperatorNode;
            if (operatorNode == null)
                return result;

            var left = operatorNode.Left;
            var right = operatorNode.Right;

            if (left is SingleValuePropertyAccessNode)
                return BuildFromPropertyNode(left as SingleValuePropertyAccessNode, right, operatorNode.OperatorKind);

            if (left is ConvertNode)
            {
                var leftSource = ((ConvertNode)left).Source;

                if (leftSource is SingleValuePropertyAccessNode)
                    return BuildFromPropertyNode(leftSource as SingleValuePropertyAccessNode, right, operatorNode.OperatorKind);

                result += BuildWhereClause(leftSource);
            }

            if (left is BinaryOperatorNode)
            {
                result += BuildWhereClause(left);
            }

            result += " " + operatorNode.OperatorKind;

            if (right is BinaryOperatorNode)
            {
                result += " " + BuildWhereClause(right);
            }

            return result;
        }

        private string BuildWhereClause(FilterQueryOption filterQueryOption)
        {
            if (filterQueryOption == null)
                return string.Empty;

            var whereClause = BuildWhereClause(filterQueryOption.FilterClause.Expression);
            return whereClause;
        }

        private bool HasDeclareKey()
        {
            if (_edmEntityType.DeclaredKey == null)
                return false;

            return _edmEntityType.DeclaredKey.Any();
        }

        private string BuildOrderClause(OrderByQueryOption orderByQueryOption)
        {
            var orderClause = string.Empty;

            if (orderByQueryOption != null)
            {
                var columns = orderByQueryOption.RawValue.Split(',').AsEnumerable();
                orderClause = string.Join(",", columns);
            }
            else
            {
                var hasDeclareKey = HasDeclareKey();
                if (hasDeclareKey)
                {
                    var keys = _edmEntityType.DeclaredKey;
                    orderClause = string.Join(",", keys.Select(k => k.Name));
                }
                else
                {
                    var firstProperty = _edmEntityType.DeclaredProperties.FirstOrDefault();
                    if (firstProperty != null)
                        orderClause = firstProperty.Name;
                }
            }

            return orderClause;
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

        private string BuildOrderClause(OrderByQueryOption orderByQueryOption, TopQueryOption topQueryOption, SkipQueryOption skipQueryOption)
        {
            string orderClause = BuildOrderClause(orderByQueryOption);
            string skipTopClause = BuildSkipTopClause(topQueryOption, skipQueryOption);

            if (string.IsNullOrEmpty(skipTopClause))
                return orderClause;

            return $"{orderClause} {skipTopClause}";
        }

        public string ToSql()
        {
            string fromClause = FromClause();
            string selectClause = BuildSelectClause(_queryOptions.SelectExpand);

            string sql = $@"SELECT {selectClause} FROM {fromClause}";

            string whereClause = BuildWhereClause(_queryOptions.Filter);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql = $"{sql} WHERE {whereClause}";
            }

            string orderClause = BuildOrderClause(_queryOptions.OrderBy, _queryOptions.Top, _queryOptions.Skip);
            if (!string.IsNullOrEmpty(orderClause))
            {
                sql = $"{sql} ORDER BY {orderClause}";
            }

            return sql;
        }

        public string ToCountSql()
        {
            string fromClause = FromClause();
            string sql = $@"SELECT COUNT(*) FROM {fromClause}";

            string whereClause = BuildWhereClause(_queryOptions.Filter);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql = $"{sql} WHERE {whereClause}";
            }

            return sql;
        }
    }
}