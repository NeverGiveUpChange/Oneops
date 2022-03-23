using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneOpsClient.Api.EF_Sqlite;
using System;
using System.Linq;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OneOpsClient.Api.Controllers
{
    [Route("api/server")]
    [ApiController]
    [Authorize]
    public class ServerController : ControllerBase
    {
        IHttpContextAccessor httpContextAccessor;
        readonly OneopsContext oneopsContext;
        string userName;
        string env;
        public ServerController(IHttpContextAccessor httpContextAccessor, OneopsContext oneopsContext)
        {
            this.httpContextAccessor = httpContextAccessor;
            userName = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Name))?.Value;
            env = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Role))?.Value;
            this.oneopsContext = oneopsContext;
        }
        /// <summary>
        /// 获取服务器列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("serverlist")]
        public JsonResult ServerList([FromQuery] string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {

                var serverlist = oneopsContext.ServerInfos.Where(x => x.Env == env&&!x.IsDelete).Select(x => new { Id = x.Id, Ip = x.Ip, CreateTime = x.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") }).ToList();
                var result = new { Code = 0, Msg = "", Count = serverlist.Count(), Data = serverlist };
                return new JsonResult(result);
            }
            else {
                var serverlist = oneopsContext.ServerInfos.Where(x => x.Env == env&&x.Ip==ip&&!x.IsDelete).Select(x => new { Id = x.Id, Ip = x.Ip, CreateTime = x.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") }).ToList();
                var result = new { Code = 0, Msg = "", Count = serverlist.Count(), Data = serverlist };
                return new JsonResult(result);
            }
        }
        /// <summary>
        /// 添加服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        [HttpPost("add")]
        public JsonResult AddServer([FromQuery] string ip)
        {
            if (userName != "chenlong") {
                return new JsonResult(new { IsSuccess = false, Message = "无权限" });
            }
            if (oneopsContext.ServerInfos.Any(x => x.Env == env && x.Ip == ip&&!x.IsDelete))
            {
                return new JsonResult(new { IsSuccess = false, Message = "此环境已有该服务器" });
            }
            else
            {
                oneopsContext.ServerInfos.Add(new ServerInfo { Ip = ip, CreateTime = DateTime.Now, UpdateTime = DateTime.Now, IsDelete = false, Env = env });
                oneopsContext.SaveChanges();
                return new JsonResult(new { IsSuccess = false, Message = "此环境成功加入该服务器" });
            }
        }
        /// <summary>
        /// 删除服务器
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost("delete/{id}")]
        public JsonResult DeleteServer([FromRoute]int id) {
            if (userName != "chenlong")
            {
                return new JsonResult(new { IsSuccess = false, Message = "无权限" });
            }
            var server = oneopsContext.ServerInfos.Where(x => x.Env == env && !x.IsDelete && x.Id == id).FirstOrDefault();
            if (server != null) {
                oneopsContext.ServerInfos.Remove(server);
                oneopsContext.SaveChanges();
            }
            return new JsonResult(new { IsSuccess = true, Messahe = "删除成功" });
        }
    }
}
