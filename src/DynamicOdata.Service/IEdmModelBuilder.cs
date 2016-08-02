using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service
{
    public interface IEdmModelBuilder
    {
        EdmModel GetModel();
    }
}
