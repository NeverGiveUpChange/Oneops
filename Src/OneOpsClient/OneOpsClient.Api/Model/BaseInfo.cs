using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class BaseInfo
    {
        [JsonIgnore]
        public string UserName { get; set; }
        [JsonIgnore]
        public string Env { get; set; }
    }
}
