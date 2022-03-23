using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneOpsServer.Api.Model
{
    public class BaseInfo
    {

        public string UserName { get; set; }

        public string Env { get; set; }
    }
}
