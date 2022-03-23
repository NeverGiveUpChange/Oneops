using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.RedisQueue
{
    public class MessageModel
    {
        public string MethodName { get; set; }
        public string UserName { get; set; }
        public string StepMessage { get; set; }
    }
}
