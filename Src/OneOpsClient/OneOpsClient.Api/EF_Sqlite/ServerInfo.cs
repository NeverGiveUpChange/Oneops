using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.EF_Sqlite
{
    public class ServerInfo
    {
        public long Id { get; set; }
        public string Ip { get; set; }
        public string Env { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool IsDelete { get; set; }

    }
}
