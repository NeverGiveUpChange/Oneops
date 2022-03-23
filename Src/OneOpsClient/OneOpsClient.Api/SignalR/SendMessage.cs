using Microsoft.AspNetCore.SignalR;
using OneOpsClient.Api.RabbitMqListener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.SignalR
{
    public class SendMessage
    {
        IHubContext<SiteMessageHub> hubContext;
        public SendMessage(IHubContext<SiteMessageHub> hubContext) {
            this.hubContext = hubContext;
        }
        public async Task SendSiteMessage(MessageModel messageModel) {

            await hubContext.Clients.Groups(messageModel.UserName).SendAsync(messageModel.MethodName,new { status=messageModel.Status,stepMessage= messageModel.StepMessage } );
        }
    }
}
