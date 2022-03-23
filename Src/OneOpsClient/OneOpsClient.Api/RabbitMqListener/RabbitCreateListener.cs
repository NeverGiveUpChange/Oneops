using Microsoft.AspNetCore.SignalR;
using OneOpsClient.Api.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneOpsClient.Api.RabbitMqListener
{
    public class RabbitCreateListener: RabbitBaseListener
    {
        IHubContext<SiteMessageHub> hubContext;
        OneopsSetting oneopsSetting;
        public RabbitCreateListener(IHubContext<SiteMessageHub> hubContext, OneopsSetting oneopsSetting):base(oneopsSetting)
        {
            base.RouteKey = oneopsSetting.RabbitSetting.CreateQueue.RouteKey;
            base.QueueName = oneopsSetting.RabbitSetting.CreateQueue.QueueName;
            this.hubContext = hubContext;
            this.oneopsSetting = oneopsSetting;
        }

        public override bool Process(MessageModel messageModel)
        {
         
            hubContext.Clients.Groups(messageModel.UserName).SendAsync(messageModel.MethodName, messageModel.StepMessage).Wait();
            return true;
        }
    }
}
