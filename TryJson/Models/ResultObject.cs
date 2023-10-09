using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryJson.Models
{
    class ResultObject
    {
        public string Path { get; set; }
        public string ColumnName { get; set; }
        public string OrgValue { get; set; }
        public string NewValue { get; set; }

    }
}
