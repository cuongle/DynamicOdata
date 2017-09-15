using System.Web.Http.OData.Query;

namespace DynamicOdata.Service.Impl.SqlBuilders
{
  public class SupportedODataQueryOptions
  {
    public static ODataValidationSettings GetDefaultDataServiceV2()
    {
      var oDataValidationSettings = new ODataValidationSettings();
      oDataValidationSettings.AllowedArithmeticOperators = AllowedArithmeticOperators.None;
      oDataValidationSettings.AllowedFunctions = AllowedFunctions.StartsWith
                                                 | AllowedFunctions.EndsWith
                                                 | AllowedFunctions.Substring
                                                 | AllowedFunctions.SubstringOf;
      oDataValidationSettings.AllowedLogicalOperators = AllowedLogicalOperators.All;
      oDataValidationSettings.AllowedQueryOptions = AllowedQueryOptions.Filter
                                                    | AllowedQueryOptions.InlineCount
                                                    | AllowedQueryOptions.OrderBy
                                                    | AllowedQueryOptions.Skip
                                                    | AllowedQueryOptions.Top;

      return oDataValidationSettings;
    }
  }
}