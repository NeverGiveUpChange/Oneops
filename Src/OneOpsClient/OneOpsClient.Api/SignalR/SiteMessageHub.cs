using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OneOpsClient.Api.SignalR
{
    public class SiteMessageHub : Hub
    {
        Dictionary<string, string> keyValuePairs;

        public SiteMessageHub()
        {
            keyValuePairs = new Dictionary<string, string>();
  
        }
        public async override Task OnConnectedAsync()
        {
            var connid = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            var userName = httpContext.Request.Headers["userName"].ToString();

            if (!keyValuePairs.ContainsKey(userName))
            {
                await Groups.AddToGroupAsync(connid, userName);
            }
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var userName = httpContext.Request.Headers["userName"].ToString();
            var connid = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connid, userName);

        }
    }
}
