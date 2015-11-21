using System.Collections.Generic;
using DynamicOdata.Service.Models;

namespace DynamicOdata.Service
{
    public interface ISchemaReader
    {
        IEnumerable<DatabaseTable> GetTables(IEnumerable<TableInfo> tableInfos);
    }
}
