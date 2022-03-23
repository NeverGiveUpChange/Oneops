using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class ExcuteResponseInfo
    {
        public string StepId { get; set; }
        public bool Status { get; set; }
        public string Ip { get; set; }
    }
}
