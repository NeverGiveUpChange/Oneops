using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.EF_Sqlite
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool IsDelete { get; set; }
    }
}
