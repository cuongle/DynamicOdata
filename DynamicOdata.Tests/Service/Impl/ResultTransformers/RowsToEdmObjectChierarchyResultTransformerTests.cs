using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.OData;
using DynamicOdata.Service.Impl.ResultTransformers;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.Edm.Library.Values;
using NUnit.Framework;

namespace DynamicOdata.Tests.Service.Impl.ResultTransformers
{
  [TestFixture]
  public class RowsToEdmObjectChierarchyResultTransformerTests
  {
    private RowsToEdmObjectChierarchyResultTransformer _sut;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      _sut = new RowsToEdmObjectChierarchyResultTransformer('.');
    }

    [Test]
    public void Translate_EnumerableOfRows_ReturnChierarchicalObject()
    {
      // Arrange
      DateTime acceptanceDateSet = DateTime.Now;
      int versionExpected = 2324234;
      string acceptanceTextSet = "Agreement text.";
      string nameSet = "TestName";
      string surnameSet = "TestSurname";

      string namePropertyName = "Name";
      string surnamePropertyName = "Surname";
      string acceptedagreementinfoTypeName = "AcceptedAgreementInfo";
      string marketingagreementTypeName = "MarketingAgreement";
      string agreementsTypeName = "Agreements";
      string acceptancedatePropertyName = "AcceptanceDate";
      string textPropertyName = "Text";
      string versionPropertyName = "Version";

      Dictionary<string, object> row = new Dictionary<string, object>();
      row.Add(namePropertyName, nameSet);
      row.Add(surnamePropertyName, surnameSet);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptancedatePropertyName}", acceptanceDateSet);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptedagreementinfoTypeName}.{versionPropertyName}", versionExpected);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptedagreementinfoTypeName}.{textPropertyName}", acceptanceTextSet);

      EdmComplexType acceptedAgreementInfo = new EdmComplexType("dbo", acceptedagreementinfoTypeName);
      acceptedAgreementInfo.AddStructuralProperty(textPropertyName, EdmPrimitiveTypeKind.String, true);
      acceptedAgreementInfo.AddStructuralProperty(versionPropertyName, EdmPrimitiveTypeKind.Int32, true);

      EdmComplexType marketingAgreement = new EdmComplexType("dbo", marketingagreementTypeName);
      marketingAgreement.AddStructuralProperty(acceptancedatePropertyName, EdmPrimitiveTypeKind.DateTime, true);

      EdmComplexType agreements = new EdmComplexType("dbo", agreementsTypeName);

      EdmEntityType entity = new EdmEntityType("dbo", "TestEntity");
      entity.AddStructuralProperty(namePropertyName, EdmPrimitiveTypeKind.String, true);
      entity.AddStructuralProperty(surnamePropertyName, EdmPrimitiveTypeKind.String, true);

      entity.AddStructuralProperty(agreementsTypeName, new EdmComplexTypeReference(agreements, true));
      agreements.AddStructuralProperty(marketingagreementTypeName, new EdmComplexTypeReference(marketingAgreement, true));
      marketingAgreement.AddStructuralProperty(acceptedagreementinfoTypeName, new EdmComplexTypeReference(acceptedAgreementInfo, true));

      EdmCollectionType edmCollectionType = new EdmCollectionType(new EdmEntityTypeReference(entity, false));

      var list = new List<IDictionary<string, object>> { row };

      // Act
      var edmEntityObject = _sut.Translate(list, edmCollectionType);

      // Assert
      var edmStructuredObject = edmEntityObject[0] as EdmStructuredObject;
      object value = null;
      edmStructuredObject.TryGetPropertyValue(namePropertyName, out value);
      Assert.AreEqual(nameSet, value);

      edmStructuredObject.TryGetPropertyValue(surnamePropertyName, out value);
      Assert.AreEqual(surnameSet, value);

      object agreement = null;
      edmStructuredObject.TryGetPropertyValue(agreementsTypeName, out agreement);

      object marketingAgreementProperty = null;
      ((EdmStructuredObject)agreement).TryGetPropertyValue(marketingagreementTypeName, out marketingAgreementProperty);

      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(acceptancedatePropertyName, out value);
      Assert.AreEqual(acceptanceDateSet, value);

      object acceptanceAgreementInfoProperty = null;
      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(acceptedagreementinfoTypeName, out acceptanceAgreementInfoProperty);

      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(versionPropertyName, out value);
      Assert.AreEqual(versionExpected, value);
      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(textPropertyName, out value);
      Assert.AreEqual(acceptanceTextSet, value);
    }

    [Test]
    public void Translate_SingleRow_ReturnChierarchicalObject()
    {
      // Arrange
      DateTime acceptanceDateSet = DateTime.Now;
      int versionExpected = 2324234;
      string acceptanceTextSet = "Agreement text.";
      string nameSet = "TestName";
      string surnameSet = "TestSurname";

      string namePropertyName = "Name";
      string surnamePropertyName = "Surname";
      string acceptedagreementinfoTypeName = "AcceptedAgreementInfo";
      string marketingagreementTypeName = "MarketingAgreement";
      string agreementsTypeName = "Agreements";
      string acceptancedatePropertyName = "AcceptanceDate";
      string textPropertyName = "Text";
      string versionPropertyName = "Version";

      Dictionary<string, object> row = new Dictionary<string, object>();
      row.Add(namePropertyName, nameSet);
      row.Add(surnamePropertyName, surnameSet);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptancedatePropertyName}", acceptanceDateSet);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptedagreementinfoTypeName}.{versionPropertyName}", versionExpected);
      row.Add($"{agreementsTypeName}.{marketingagreementTypeName}.{acceptedagreementinfoTypeName}.{textPropertyName}", acceptanceTextSet);

      EdmComplexType acceptedAgreementInfo = new EdmComplexType("dbo", acceptedagreementinfoTypeName);
      acceptedAgreementInfo.AddStructuralProperty(textPropertyName, EdmPrimitiveTypeKind.String, true);
      acceptedAgreementInfo.AddStructuralProperty(versionPropertyName, EdmPrimitiveTypeKind.Int32, true);

      EdmComplexType marketingAgreement = new EdmComplexType("dbo", marketingagreementTypeName);
      marketingAgreement.AddStructuralProperty(acceptancedatePropertyName, EdmPrimitiveTypeKind.DateTime, true);

      EdmComplexType agreements = new EdmComplexType("dbo", agreementsTypeName);

      EdmEntityType entity = new EdmEntityType("dbo", "TestEntity");
      entity.AddStructuralProperty(namePropertyName, EdmPrimitiveTypeKind.String, true);
      entity.AddStructuralProperty(surnamePropertyName, EdmPrimitiveTypeKind.String, true);

      entity.AddStructuralProperty(agreementsTypeName, new EdmComplexTypeReference(agreements, true));
      agreements.AddStructuralProperty(marketingagreementTypeName, new EdmComplexTypeReference(marketingAgreement, true));
      marketingAgreement.AddStructuralProperty(acceptedagreementinfoTypeName, new EdmComplexTypeReference(acceptedAgreementInfo, true));

      // Act
      var edmStructuredObject = _sut.Translate(row, entity);

      // Assert
      object value = null;
      edmStructuredObject.TryGetPropertyValue(namePropertyName, out value);
      Assert.AreEqual(nameSet, value);

      edmStructuredObject.TryGetPropertyValue(surnamePropertyName, out value);
      Assert.AreEqual(surnameSet, value);

      object agreement = null;
      edmStructuredObject.TryGetPropertyValue(agreementsTypeName, out agreement);

      object marketingAgreementProperty = null;
      ((EdmStructuredObject)agreement).TryGetPropertyValue(marketingagreementTypeName, out marketingAgreementProperty);

      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(acceptancedatePropertyName, out value);
      Assert.AreEqual(acceptanceDateSet, value);

      object acceptanceAgreementInfoProperty = null;
      ((EdmStructuredObject)marketingAgreementProperty).TryGetPropertyValue(acceptedagreementinfoTypeName, out acceptanceAgreementInfoProperty);

      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(versionPropertyName, out value);
      Assert.AreEqual(versionExpected, value);
      ((EdmStructuredObject)acceptanceAgreementInfoProperty).TryGetPropertyValue(textPropertyName, out value);
      Assert.AreEqual(acceptanceTextSet, value);
    }
  }
}