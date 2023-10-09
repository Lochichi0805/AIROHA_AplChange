using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryJson.Models
{
    public class ResultObject
    {
        public ResultObject()
        { }

        public ResultObject(string reqID, string path, string columnName, string orgValue, string newValue)
        {
            this.ReqID = reqID;
            this.Path = path;
            this.ColumnName = columnName;
            this.OrgValue = orgValue;
            this.NewValue = newValue;
        }


        public string ReqID { get; set; }
        public string Path { get; set; }
        public string ColumnName { get; set; }
        public string OrgValue { get; set; }
        public string NewValue { get; set; }

    }
}
