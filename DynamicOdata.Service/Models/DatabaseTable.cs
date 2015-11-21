using System.Collections.Generic;

namespace DynamicOdata.Service.Models
{
    public class DatabaseTable
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public IEnumerable<DatabaseColumn> Columns { get; set; }
    }
}
