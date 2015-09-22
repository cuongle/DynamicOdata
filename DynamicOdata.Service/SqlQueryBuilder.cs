using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Query;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service
{
    public class SqlQueryBuilder
    {
        private readonly ODataQueryOptions _oDataQueryOptions;
        private readonly EdmEntityType _edmEntityType;

        public SqlQueryBuilder(EdmEntityType edmEntityType, ODataQueryOptions oDataQueryOptions)
        {
            _oDataQueryOptions = oDataQueryOptions;
            _edmEntityType = edmEntityType;
        }

        private string BuildSelectClause(SelectExpandQueryOption selectExpandQueryOption)
        {
            if (selectExpandQueryOption == null)
                return "*"; // Select All

            return selectExpandQueryOption.RawSelect;
        }

        private string FromClause()
        {
            return $"[{_edmEntityType.Namespace}].[{_edmEntityType.Name}]";
        }

        private string BuildWhereClause(FilterQueryOption filterQueryOption)
        {
            if (filterQueryOption == null) return null;

            var operatorMap = new Dictionary<string, string>
            {
                {"eq", "="},
                {"ne", "!="},
                {"gt", ">"},
                {"ge", ">="},
                {"lt", "<"},
                {"le", "<="}
            };

            string odataClause = operatorMap.Aggregate(filterQueryOption.RawValue,
                (current, pair) => current.Replace(pair.Key, pair.Value));

            return odataClause;
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
                orderClause = orderByQueryOption.RawValue;
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
                int skipValue = skipQueryOption != null ? skipQueryOption.Value : 0;
                skipTopClause = string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", skipValue, topQueryOption.Value);
            }

            if (topQueryOption == null && skipQueryOption != null)
            {
                skipTopClause = string.Format("OFFSET {0} ROWS", skipQueryOption.Value);
            }

            return skipTopClause;
        }

        private string BuildOrderClause(OrderByQueryOption orderByQueryOption, TopQueryOption topQueryOption, SkipQueryOption skipQueryOption)
        {
            string orderClause = BuildOrderClause(orderByQueryOption);
            string skipTopClause = BuildSkipTopClause(topQueryOption, skipQueryOption);

            if (string.IsNullOrEmpty(skipTopClause))
                return orderClause;

            return string.Format("{0} {1}", orderClause, skipTopClause);
        }

        public string ToSql()
        {
            string fromClause = FromClause();
            string selectClause = BuildSelectClause(_oDataQueryOptions.SelectExpand);

            string sql = string.Format(@"SELECT {0} FROM {1}", selectClause, fromClause);

            string whereClause = BuildWhereClause(_oDataQueryOptions.Filter);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql = string.Format("{0} WhERE {1}", sql, whereClause);
            }

            string orderClause = BuildOrderClause(_oDataQueryOptions.OrderBy, _oDataQueryOptions.Top, _oDataQueryOptions.Skip);
            if (!string.IsNullOrEmpty(orderClause))
            {
                sql = string.Format("{0} ORDER BY {1}", sql, orderClause);
            }

            return sql;
        }

        public string ToCountSql()
        {
            string fromClause = FromClause();
            string sql = string.Format(@"SELECT COUNT(*) FROM {0}", fromClause);

            string whereClause = BuildWhereClause(_oDataQueryOptions.Filter);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql = string.Format("{0} WhERE {1}", sql, whereClause);
            }

            return sql;
        }
    }
}