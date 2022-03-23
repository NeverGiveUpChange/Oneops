using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.RabbitMqListener
{
    public class MessageModel
    {
        public string MethodName { get; set; }
        public string UserName { get; set; }
        public string StepMessage { get; set; }
       public bool Status { get; set; }
    }
}
