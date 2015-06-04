using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Edm.Library;

namespace DynamicOdata.Service
{
    public interface IEdmModelBuilder
    {
        EdmModel GetModel();
    }
}
