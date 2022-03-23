using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OneOpsClient.Api.RedisQueue;
using OneOpsClient.Api.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneOpsClient.Api.RedisQueue
{
    public class PublishQueue : BackgroundService
    {
        IHubContext<SiteMessageHub> hubContext;
        OneopsSetting oneopsSetting;
        public PublishQueue(IHubContext<SiteMessageHub> hubContext, OneopsSetting oneopsSetting)
        {
            this.hubContext = hubContext;
            this.oneopsSetting = oneopsSetting;
        }
      

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = RedisHelper.BLPop<string>(oneopsSetting.RedisQueueSetting.QueueWaitTime, oneopsSetting.RedisQueueSetting.PublishQueueName);
                if (message != null)
                {

                    var messageModel = JsonConvert.DeserializeObject<MessageModel>(message);

                    hubContext.Clients.Groups(messageModel.UserName).SendAsync(messageModel.MethodName, messageModel.StepMessage).Wait();
                }
                await Task.Delay(oneopsSetting.RedisQueueSetting.ThreadWaitTime);

            }
        }

    }
}
