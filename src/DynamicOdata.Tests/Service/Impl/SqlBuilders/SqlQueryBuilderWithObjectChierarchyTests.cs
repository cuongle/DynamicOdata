using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using DynamicOdata.Service.Impl.SqlBuilders;
using DynamicOdata.Tests.Service.Impl.ResultTransformers;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Data.Edm;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl.SqlBuilders
{
  [TestFixture]
  public class SqlQueryBuilderWithObjectHierarchyTests
  {
    private ODataQueryContext _oDataQueryContext;
    private SqlQueryBuilderWithObjectHierarchy _sut;

    [TestFixtureSetUp]
    public void OneTimeSetUp()
    {
      _sut = new SqlQueryBuilderWithObjectHierarchy('.');

      var edmModel = TestModelBuilder.BuildModel();
      var edmSchemaType = edmModel.SchemaElements.FirstOrDefault(w => w.Name == TestModelBuilder.TestEntityName) as IEdmSchemaType;
      _oDataQueryContext = new ODataQueryContext(edmModel, edmSchemaType);
    }

    [Test]
    public void ToSql_OrderByHierarchicalNamePassed_ItIsReplacedInSql()
    {
      // Arrange
      string fieldName = $"{TestModelBuilder.TestEntityName_AgreementsTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName}";
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$orderby={fieldName} desc");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"order by [{fieldName.Replace("/", ".")}] desc", StringComparison.InvariantCultureIgnoreCase));
    }

    [Test]
    public void ToSql_OrderByDesc_ReturnsOrderAscInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$orderby={TestModelBuilder.TestEntityName_NamePropertyName} asc");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"order by [{TestModelBuilder.TestEntityName_NamePropertyName}] asc", StringComparison.InvariantCultureIgnoreCase));
    }

    [Test]
    public void ToSql_OrderByDesc_ReturnsOrderDescInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$orderby={TestModelBuilder.TestEntityName_NamePropertyName} desc");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"order by [{TestModelBuilder.TestEntityName_NamePropertyName}] desc", StringComparison.InvariantCultureIgnoreCase));
    }

    [Test]
    public void ToSql_OrderByNothingPassed_ContainsOrderByKeyInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"order by [{TestModelBuilder.TestEntityName_IdPropertyName}]", StringComparison.InvariantCultureIgnoreCase), sqlQuery.Query);
    }

    [Test]
    public void ToSql_Skip12_ReturnsSkip12InSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$skip=12");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"OFFSET 12 ROWS", StringComparison.InvariantCultureIgnoreCase), sqlQuery.Query);
    }

    [Test]
    public void ToSql_Top12_ReturnsTop12InSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$top=12");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"OFFSET 0 ROWS FETCH NEXT 12 ROWS ONLY", StringComparison.InvariantCultureIgnoreCase), sqlQuery.Query);
    }

    [Test]
    public void ToSql_Top12Skip8_ReturnsTop12Skip8InSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$skip=8&$top=12");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsTrue(sqlQuery.Query.EndsWith($"OFFSET 8 ROWS FETCH NEXT 12 ROWS ONLY", StringComparison.InvariantCultureIgnoreCase), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereHierarchicalNamePassed_ItIsReplacedInSql()
    {
      // Arrange
      string fieldName = $"{TestModelBuilder.TestEntityName_AgreementsTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName}";
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={fieldName} eq datetime'2016-01-01'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (DateTime)f.Value == new DateTime(2016, 01, 01));
      Assert.NotNull(firstOrDefault);
      var searchableString = $"[{firstOrDefault.Key.Replace('_', '.').Substring(0, firstOrDefault.Key.LastIndexOf('_'))}] = @{firstOrDefault.Key}";
      Assert.IsTrue(sqlQuery.Query.Contains(searchableString), $"Query => <{sqlQuery.Query}>, searching for = <{searchableString}");
    }

    [Test]
    public void ToSql_WhereNameAddedTwoTimesEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz' and {TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereNameEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereNameEqualsAndSurnameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz' and {TestModelBuilder.TestEntityName_SurnamePropertyName} ne 'qaz'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      var firstOrDefault2 = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "qaz");
      Assert.NotNull(firstOrDefault2);

      Assert.IsTrue(
        sqlQuery.Query.ToLower()
          .Contains(
            $"(([{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}))  and (([{TestModelBuilder.TestEntityName_SurnamePropertyName}] != @{firstOrDefault2.Key}))".ToLower()),
        sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereNameEqualsOrSurnameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz' or {TestModelBuilder.TestEntityName_SurnamePropertyName} ne 'qaz'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      var firstOrDefault2 = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "qaz");
      Assert.NotNull(firstOrDefault2);

      Assert.IsTrue(
        sqlQuery.Query.ToLower()
          .Contains(
            $"(([{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}))  or (([{TestModelBuilder.TestEntityName_SurnamePropertyName}] != @{firstOrDefault2.Key}))".ToLower()),
        sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereNameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} ne 'xyz'");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] != @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereValueGreather_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} gt 1");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 1);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] > @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereValueGreatherEqual_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} ge 1");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 1);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] >= @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereValueLess_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} lt 42423");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 42423);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] < @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereValueLessEqual_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} le 42423");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 42423);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] <= @{firstOrDefault.Key}"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_WhereValueEqualNull_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq null");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);

      // Assert
      Assert.IsEmpty(sqlQuery.Parameters);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] IS NULL"), sqlQuery.Query);
    }

    [Test]
    public void ToSql_FilterContainsBrackets_SqlWhereClauseContainsBrackets()
    {
      var idProperty = TestModelBuilder.TestEntityName_IdPropertyName;
      var nameProperty = TestModelBuilder.TestEntityName_NamePropertyName;
      var filter = $@"({ idProperty } lt 10 or { idProperty } gt 100) and { nameProperty } eq null";

      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($@"{"http://localhost:81"}/{TestModelBuilder.TestEntityName}?$filter={filter}");

      // Act
      var sqlQuery = _sut.ToSql(oDataQueryOptions);
      var param1 = sqlQuery.Parameters.First(x => 10 == (int) x.Value).Key;
      var param2 = sqlQuery.Parameters.First(x => 100 == (int) x.Value).Key;

      // Assert
      var normalizedWhitespaceSql = Regex.Replace(sqlQuery.Query, @"\s+", " ");
      Assert.IsTrue(
        normalizedWhitespaceSql.Contains($"WHERE ( (([{idProperty}] < @{param1})) Or (([{idProperty}] > @{param2})) ) And"),
        normalizedWhitespaceSql);
    }

    [Test]
    public void ToSqlCount_WhereHierarchicalNamePassed_ItIsReplacedInSql()
    {
      // Arrange
      string fieldName = $"{TestModelBuilder.TestEntityName_AgreementsTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}/{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName}";
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={fieldName} eq datetime'2016-01-01'&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (DateTime)f.Value == new DateTime(2016, 01, 01));
      Assert.NotNull(firstOrDefault);
      var searchableString = $"[{firstOrDefault.Key.Replace('_', '.').Substring(0, firstOrDefault.Key.LastIndexOf('_'))}] = @{firstOrDefault.Key}";
      Assert.IsTrue(sqlQuery.Query.Contains(searchableString), $"Query => <{sqlQuery.Query}>, searching for = <{searchableString}");
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereNameEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz'&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereNameEqualsAndSurnameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz' and {TestModelBuilder.TestEntityName_SurnamePropertyName} ne 'qaz'&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      var firstOrDefault2 = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "qaz");
      Assert.NotNull(firstOrDefault2);

      Assert.IsTrue(
        sqlQuery.Query.ToLower()
          .Contains(
            $"(([{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}))  and (([{TestModelBuilder.TestEntityName_SurnamePropertyName}] != @{firstOrDefault2.Key}))".ToLower()),
        sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereNameEqualsOrSurnameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} eq 'xyz' or {TestModelBuilder.TestEntityName_SurnamePropertyName} ne 'qaz'&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      var firstOrDefault2 = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "qaz");
      Assert.NotNull(firstOrDefault2);

      Assert.IsTrue(
        sqlQuery.Query.ToLower()
          .Contains(
            $"(([{TestModelBuilder.TestEntityName_NamePropertyName}] = @{firstOrDefault.Key}))  or (([{TestModelBuilder.TestEntityName_SurnamePropertyName}] != @{firstOrDefault2.Key}))".ToLower()),
        sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereNameNotEquals_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_NamePropertyName} ne 'xyz'&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (string)f.Value == "xyz");
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_NamePropertyName}] != @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereValueGreather_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} gt 1&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 1);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] > @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereValueGreatherEqual_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} ge 1&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 1);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] >= @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereValueLess_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} lt 42423&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 42423);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] < @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void ToSqlCount_WhereValueLessEqual_ReturnsWhereInSql()
    {
      // Arrange
      var oDataQueryOptions = CreateODataQueryOptions($"http://localhost:81/{TestModelBuilder.TestEntityName}?$filter={TestModelBuilder.TestEntityName_IdPropertyName} le 42423&$inlinecount=allpages");

      // Act
      var sqlQuery = _sut.ToCountSql(oDataQueryOptions);

      // Assert
      var firstOrDefault = sqlQuery.Parameters.FirstOrDefault(f => (int)f.Value == 42423);
      Assert.NotNull(firstOrDefault);
      Assert.IsTrue(sqlQuery.Query.Contains($"[{TestModelBuilder.TestEntityName_IdPropertyName}] <= @{firstOrDefault.Key}"), sqlQuery.Query);
      Assert.IsTrue(sqlQuery.Query.ToLower().StartsWith($"select count(*)"), sqlQuery.Query);
    }

    [Test]
    public void SetMaxNodeCountFromConfigFile_sets_values([Values("123", "456")]string maxNodeCountCase)
    {
      ConfigurationManager.AppSettings["DynamicOData.ODataValidation.MaxNodeCount"] = maxNodeCountCase;

      SqlQueryBuilderWithObjectHierarchy myClass = new SqlQueryBuilderWithObjectHierarchy('.');

      var method = myClass.GetType()
        .GetMethod("SetMaxNodeCountFromConfigFile", BindingFlags.Static | BindingFlags.NonPublic);
      method.Invoke(null, null);

      Assert.That(SqlQueryBuilderWithObjectHierarchy.GetSupportedODataQueryOptions().MaxNodeCount, Is.EqualTo(int.Parse(maxNodeCountCase)));
    }

    [Test]
    public void SqlQueryBuilderWithObjectHierarchy_Use_SupportedODataQueryOptions_GetDefaultDataServiceV2()
    {
      SqlQueryBuilderWithObjectHierarchy myClass = new SqlQueryBuilderWithObjectHierarchy('.');
      var getSupportedODataQueryOptions = myClass.GetType().GetMethod("GetSupportedODataQueryOptions", BindingFlags.Static | BindingFlags.Public);

      var compareResult = new CompareLogic().Compare(getSupportedODataQueryOptions.Invoke(null, null),
          SupportedODataQueryOptions.GetDefaultDataServiceV2());
      Assert.IsTrue(compareResult.AreEqual);
    }

    [Test]
    public void SqlQueryBuilderWithObjectHierarchy_Use_SupportedODataQueryOptions_GetDefaultDataServiceV2_Contains_SubstringOf()
    {
      SqlQueryBuilderWithObjectHierarchy myClass = new SqlQueryBuilderWithObjectHierarchy('.');
      var getSupportedODataQueryOptions = myClass.GetType().GetMethod("GetSupportedODataQueryOptions", BindingFlags.Static | BindingFlags.Public);

      var allowedFunctions = ((ODataValidationSettings)getSupportedODataQueryOptions.Invoke(null, null)).AllowedFunctions;

     Assert.IsTrue(allowedFunctions.HasFlag(AllowedFunctions.SubstringOf));
    }

    [TestCase(AllowedArithmeticOperators.Add, AllowedFunctions.All, AllowedLogicalOperators.Equal, AllowedQueryOptions.Filter)]
    [TestCase(AllowedArithmeticOperators.Add | AllowedArithmeticOperators.Modulo, AllowedFunctions.Cast | AllowedFunctions.Concat, AllowedLogicalOperators.Equal, AllowedQueryOptions.Filter)]
    [TestCase(AllowedArithmeticOperators.Add | AllowedArithmeticOperators.Modulo, AllowedFunctions.Cast | AllowedFunctions.Concat, AllowedLogicalOperators.Equal, AllowedQueryOptions.Filter | AllowedQueryOptions.Format)]
    public void SqlQueryBuilderWithObjectHierarchy_Use_SupportedODataQueryOptions_from_parameter(AllowedArithmeticOperators allowedArithmeticOperators, AllowedFunctions allowedFunctions, AllowedLogicalOperators allowedLogicalOperators, AllowedQueryOptions allowedQueryOptions)
    {
      ODataValidationSettings dataValidationSettings = new ODataValidationSettings
      {
        AllowedArithmeticOperators = allowedArithmeticOperators,
        AllowedFunctions = allowedFunctions,
        AllowedLogicalOperators = allowedLogicalOperators,
        AllowedQueryOptions = allowedQueryOptions
      };

      SqlQueryBuilderWithObjectHierarchy myClass = new SqlQueryBuilderWithObjectHierarchy('.', dataValidationSettings);
      var getSupportedODataQueryOptions = myClass.GetType().GetMethod("GetSupportedODataQueryOptions", BindingFlags.Static | BindingFlags.Public);

      var compareResult = new CompareLogic().Compare(getSupportedODataQueryOptions.Invoke(null, null), dataValidationSettings);
      Assert.IsTrue(compareResult.AreEqual);
    }


    [Test]
    public void SetMaxNodeCountFromConfigFile_doesnt_override_value_on_wrong_value([Values("", "-1")]string maxNodeCountTestCase)
    {
      int defaultValue = SqlQueryBuilderWithObjectHierarchy.GetSupportedODataQueryOptions().MaxNodeCount;
      ConfigurationManager.AppSettings["DynamicOData.ODataValidation.MaxNodeCount"] = maxNodeCountTestCase;

      SqlQueryBuilderWithObjectHierarchy myClass = new SqlQueryBuilderWithObjectHierarchy('.');

      var method = myClass.GetType()
        .GetMethod("SetMaxNodeCountFromConfigFile", BindingFlags.Static | BindingFlags.NonPublic);
      method.Invoke(null, null);

      Assert.That(SqlQueryBuilderWithObjectHierarchy.GetSupportedODataQueryOptions().MaxNodeCount, Is.EqualTo(defaultValue));
    }

    private ODataQueryOptions CreateODataQueryOptions(string uri)
    {
      HttpRequestMessage message = new HttpRequestMessage();
      message.RequestUri = new Uri(uri);

      var oDataQueryOptions = new ODataQueryOptions(_oDataQueryContext, message);
      return oDataQueryOptions;
    }
  }
}