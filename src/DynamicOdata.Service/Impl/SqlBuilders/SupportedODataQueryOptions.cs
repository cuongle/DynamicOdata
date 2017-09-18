using System.Web.Http.OData.Query;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  public class SupportedODataQueryOptions
  {
    public static ODataValidationSettings GetDefaultDataServiceV2()
    {
      var oDataValidationSettings = new ODataValidationSettings
      {
        AllowedArithmeticOperators = AllowedArithmeticOperators.None,
        AllowedFunctions = AllowedFunctions.StartsWith
                                                 | AllowedFunctions.EndsWith
                                                 | AllowedFunctions.Substring
                                                 | AllowedFunctions.SubstringOf,
        AllowedLogicalOperators = AllowedLogicalOperators.All,
        AllowedQueryOptions = AllowedQueryOptions.Filter
                                                    | AllowedQueryOptions.InlineCount
                                                    | AllowedQueryOptions.OrderBy
                                                    | AllowedQueryOptions.Skip
                                                    | AllowedQueryOptions.Top
      };

      return oDataValidationSettings;
    }
  }
}