using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.OData;
using DynamicOdata.Service.Impl.ResultTransformers;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.Edm.Library.Values;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl.ResultTransformers
{
  [TestFixture]
  public class RowsToEdmObjectHierarchyResultTransformerTests
  {
    private RowsToEdmObjectHierarchyResultTransformer _sut;

    [SetUp]
    public void OneTimeSetUp()
    {
      _sut = new RowsToEdmObjectHierarchyResultTransformer('.');
    }

    [Test]
    public void Translate_EnumerableOfRows_ReturnHierarchicalObject()
    {
      // Arrange
      DateTime acceptanceDateSet = DateTime.Now;
      int versionExpected = 2324234;
      string acceptanceTextSet = "Agreement text.";
      string nameSet = "TestName";
      string surnameSet = "TestSurname";

      Dictionary<string, object> row = new Dictionary<string, object>();
      row.Add(TestModelBuilder.TestEntityName_NamePropertyName, nameSet);
      row.Add(TestModelBuilder.TestEntityName_SurnamePropertyName, surnameSet);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName}", acceptanceDateSet);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_VersionPropertyName}", versionExpected);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_TextPropertyName}", acceptanceTextSet);

      var model = TestModelBuilder.BuildModel();
      var entity = model.SchemaElements.FirstOrDefault(f => f.Name == TestModelBuilder.TestEntityName) as EdmEntityType;

      EdmCollectionType edmCollectionType = new EdmCollectionType(new EdmEntityTypeReference(entity, false));

      var list = new List<IDictionary<string, object>> { row };

      // Act
      var edmEntityObject = _sut.Translate(list, edmCollectionType);

      // Assert
      var edmStructuredObject = edmEntityObject[0] as EdmStructuredObject;
      AssertObject(edmStructuredObject, nameSet, surnameSet, acceptanceDateSet, versionExpected, acceptanceTextSet);
    }

    [Test]
    public void Translate_SingleRow_ReturnHierarchicalObject()
    {
      // Arrange
      DateTime acceptanceDateSet = DateTime.Now;
      int versionExpected = 2324234;
      string acceptanceTextSet = "Agreement text.";
      string nameSet = "TestName";
      string surnameSet = "TestSurname";

      Dictionary<string, object> row = new Dictionary<string, object>();
      row.Add(TestModelBuilder.TestEntityName_NamePropertyName, nameSet);
      row.Add(TestModelBuilder.TestEntityName_SurnamePropertyName, surnameSet);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName}", acceptanceDateSet);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_VersionPropertyName}", versionExpected);
      row.Add($"{TestModelBuilder.TestEntityName_AgreementsTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName}.{TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_TextPropertyName}", acceptanceTextSet);

      var model = TestModelBuilder.BuildModel();
      var entity = model.SchemaElements.FirstOrDefault(f => f.Name == TestModelBuilder.TestEntityName) as EdmEntityType;

      // Act
      var edmStructuredObject = _sut.Translate(row, entity);

      // Assert
      AssertObject(edmStructuredObject, nameSet, surnameSet, acceptanceDateSet, versionExpected, acceptanceTextSet);
    }

    private static void AssertObject(
      EdmStructuredObject edmEntityObject,
      string nameSet,
      string surnameSet,
      DateTime acceptanceDateSet,
      int versionExpected,
      string acceptanceTextSet)
    {
      object value = null;
      edmEntityObject.TryGetPropertyValue(TestModelBuilder.TestEntityName_NamePropertyName, out value);
      Assert.AreEqual(nameSet, value);

      edmEntityObject.TryGetPropertyValue(TestModelBuilder.TestEntityName_SurnamePropertyName, out value);
      Assert.AreEqual(surnameSet, value);

      object agreement = null;
      edmEntityObject.TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName, out agreement);

      object marketingAgreementProperty = null;
      ((EdmStructuredObject)agreement).TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName, out marketingAgreementProperty);

      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName, out value);
      Assert.AreEqual(acceptanceDateSet, value);

      object acceptanceAgreementInfoProperty = null;
      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName, out acceptanceAgreementInfoProperty);

      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_VersionPropertyName, out value);
      Assert.AreEqual(versionExpected, value);
      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(TestModelBuilder.TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_TextPropertyName, out value);
      Assert.AreEqual(acceptanceTextSet, value);
    }
  }
}