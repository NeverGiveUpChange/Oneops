using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OneOpsClient.Api.RabbitMqListener;
using OneOpsClient.Api.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OneOpsClient.Api.Controllers
{
    [Route("api/message")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        readonly SendMessage sendMessage;
        public MessageController(SendMessage sendMessage)
        {
            this.sendMessage = sendMessage;
        }
        [HttpPost("create")]
        public async Task CreateMessgae([FromBody] MessageModel messageModel)
        {
            await sendMessage.SendSiteMessage(messageModel);
        }


        [HttpPost("publish")]
        public async Task PublishMessage([FromBody] MessageModel messageModel)
        {
            await sendMessage.SendSiteMessage(messageModel);
        }


        [HttpPost("rollback")]
        public async Task RollbackMessage([FromBody] MessageModel messageModel)
        {
            await sendMessage.SendSiteMessage(messageModel);
        }
        [HttpPost("delete")]
        public async Task DeleteMessage([FromBody] MessageModel messageModel)
        {
            await sendMessage.SendSiteMessage(messageModel);
        }


    }
}
