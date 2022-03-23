using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsServer.Api.RedisQueue
{
    public class MessageModel
    {
        /// <summary>
        /// signalR执行方法名
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// signalR分组名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 步骤信息
        /// </summary>
        public string StepMessage { get; set; }
        /// <summary>
        /// 步骤状态
        /// </summary>

        public bool Status { get; set; } = true;

    }
}
