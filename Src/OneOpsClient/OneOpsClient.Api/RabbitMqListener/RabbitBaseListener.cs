using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OneOpsClient.Api.RabbitMqListener
{
    public class RabbitBaseListener : BackgroundService
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        protected string RouteKey;
        protected string QueueName;
        readonly OneopsSetting oneopsSetting;
        public RabbitBaseListener(OneopsSetting oneopsSetting) {
            this.oneopsSetting = oneopsSetting;
            try
            {
                var factory = new ConnectionFactory()
                {
                    // 这是我这边的配置,自己改成自己用就好
                    HostName = oneopsSetting.RabbitSetting.HostName,
                    UserName = oneopsSetting.RabbitSetting.UserName,
                    Password = oneopsSetting.RabbitSetting.Password,
                    Port = oneopsSetting.RabbitSetting.Port,
                    VirtualHost = oneopsSetting.RabbitSetting.VirtualHost
                };
                this.connection = factory.CreateConnection();
                this.channel = connection.CreateModel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitListener init error,ex:{ex.Message}");
            }
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Register();
            return Task.CompletedTask;
        }
        public virtual bool Process(MessageModel message)
        {
            throw new NotImplementedException();
        }
        public void Register()
        {
            
            channel.ExchangeDeclare(exchange: oneopsSetting.RabbitSetting.ExchangeName, type: ExchangeType.Direct);
            channel.QueueDeclare(queue: QueueName,autoDelete:false, exclusive: false);
            channel.QueueBind(queue: QueueName,
                              exchange: oneopsSetting.RabbitSetting.ExchangeName,
                              routingKey: RouteKey);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());
                var messageModel = JsonConvert.DeserializeObject<MessageModel>(message);
                var result = Process(messageModel);
                if (result)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };
            channel.BasicConsume(queue: QueueName, consumer: consumer);
        }
    }
}
