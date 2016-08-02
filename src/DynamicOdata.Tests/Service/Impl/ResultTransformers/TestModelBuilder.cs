using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Tests.Service.Impl.ResultTransformers
{
  internal class TestModelBuilder
  {
    public const string AcceptanceTextSet = "Agreement text.";
    public const string NameSet = "TestName";
    public const string SurnameSet = "TestSurname";
    public const string TestEntityName = "TestEntity";
    public const string TestEntityName_AgreementsTypeName = "Agreements";
    public const string TestEntityName_AgreementsTypeName_MarketingagreementTypeName = "MarketingAgreement";
    public const string TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName = "AcceptanceDate";
    public const string TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName = "AcceptedAgreementInfo";
    public const string TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_TextPropertyName = "Text";
    public const string TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_VersionPropertyName = "Version";
    public const string TestEntityName_IdPropertyName = "Id";
    public const string TestEntityName_NamePropertyName = "Name";
    public const string TestEntityName_SurnamePropertyName = "Surname";

    public static EdmModel BuildModel()
    {
      EdmModel edmModel = new EdmModel();

      EdmComplexType acceptedAgreementInfo = new EdmComplexType("dbo", TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName);
      acceptedAgreementInfo.AddStructuralProperty(TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_TextPropertyName, EdmPrimitiveTypeKind.String, true);
      acceptedAgreementInfo.AddStructuralProperty(TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName_VersionPropertyName, EdmPrimitiveTypeKind.Int32, true);
      edmModel.AddElement(acceptedAgreementInfo);

      EdmComplexType marketingAgreement = new EdmComplexType("dbo", TestEntityName_AgreementsTypeName_MarketingagreementTypeName);
      marketingAgreement.AddStructuralProperty(TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptanceDatePropertyName, EdmPrimitiveTypeKind.DateTime, true);
      edmModel.AddElement(marketingAgreement);

      EdmComplexType agreements = new EdmComplexType("dbo", TestEntityName_AgreementsTypeName);
      edmModel.AddElement(agreements);

      EdmEntityType entity = new EdmEntityType("dbo", TestEntityName);
      entity.AddStructuralProperty(TestEntityName_NamePropertyName, EdmPrimitiveTypeKind.String, true);
      entity.AddStructuralProperty(TestEntityName_SurnamePropertyName, EdmPrimitiveTypeKind.String, true);
      var keyProperty = entity.AddStructuralProperty(TestEntityName_IdPropertyName, EdmPrimitiveTypeKind.Int32, true);
      entity.AddKeys(keyProperty);
      entity.AddStructuralProperty(TestEntityName_AgreementsTypeName, new EdmComplexTypeReference(agreements, true));
      edmModel.AddElement(entity);

      agreements.AddStructuralProperty(TestEntityName_AgreementsTypeName_MarketingagreementTypeName, new EdmComplexTypeReference(marketingAgreement, true));
      marketingAgreement.AddStructuralProperty(TestEntityName_AgreementsTypeName_MarketingagreementTypeName_AcceptedAgreementInfoTypeName, new EdmComplexTypeReference(acceptedAgreementInfo, true));

      return edmModel;
    }
  }
}