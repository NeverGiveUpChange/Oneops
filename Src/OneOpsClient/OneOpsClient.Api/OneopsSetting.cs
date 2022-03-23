using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api
{
    public class OneopsSetting
    {
        public ApiAdress ApiAdress { get; set; }
        public JwtSetting JwtSetting { get; set; }
        public RedisQueueSetting RedisQueueSetting { get; set; }
        public SignalRSetting SignalRSetting { get; set; }

        public RabbitSetting RabbitSetting { get; set; }

    }
    public class ApiAdress
    {
        public string WebSiteInfo { get; set; }
        public string RollBackPackages { get; set; }
        public string Publish { get; set; }

        public string RollBack { get; set; }
        public string Delete { get; set; }
        public string CreateSite { get; set; }
    }
    public class JwtSetting
    {
        public string TokenSecret { get; set; }
        public string ValidIssuer { get; set; }
        public string ValidAudience { get; set; }
        public int Exp { get; set; }
    }
    public class RabbitSetting {

        public string ExchangeName { get; set; }
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }


        public CQueue CreateQueue { get; set; }
        public PQueue PublishQueue { get; set; }
        public RbQueue RollbackQueue { get; set; }

    }
    public class CQueue
    { 
        public string RouteKey { get; set; }
        public string QueueName { get; set; }
         
    }
    public class PQueue
    {
        public string RouteKey { get; set; }
        public string QueueName { get; set; }
    }
    public class RbQueue
    {
        public string RouteKey { get; set; }
        public string QueueName { get; set; }
    }
    public class RedisQueueSetting {
        public string CreateQueueName { get; set; }
        public string PublishQueueName { get; set; }
        public string RollBackQueueName { get; set; }
        public int QueueWaitTime { get; set; }
        public int ThreadWaitTime { get; set; }
    }
    public class SignalRSetting {
        public int ClientTimeoutInterval { get; set; }
        public int KeepAliveInterval { get; set; }
        public string CreateMethodName { get; set; }
        public string RollBackMethodName { get; set; }
        public string PublishMethodName { get; set; }

        public string DeleteMethodName { get; set; }
        public string EverytimeCompleteMethodName { get; set; }
        public string AllCompleteMethodName { get; set; }
    }
}
