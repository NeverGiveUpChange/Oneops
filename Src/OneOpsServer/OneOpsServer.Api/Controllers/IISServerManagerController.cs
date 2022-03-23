using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Web.Administration;
using OneOpsServer.Api.Model;

using OneOpsServer.Api.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OneOpsServer.Api.Controllers
{
    [Route("api/iisservermanager")]
    [ApiController]
    public class IISServerManagerController : ControllerBase
    {
        readonly IISManagerService iisManagerService;
        static Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "1", "Starting" }, { "2", "Started" }, { "3", "Stopping" }, { "4", "Stopped" }, { "5", "Pausing" }, { "6", "Paused" }, { "7", "Continuing" } };

        public IISServerManagerController(IISManagerService iisManagerService)
        {
            this.iisManagerService = iisManagerService;
        }
        /// <summary>
        /// 获取当前iis站点信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("websiteinfo")]

        public IActionResult GetWebSiteInfo([FromQuery] string searchSiteName, [FromQuery] string ip)
        {


            var siteInfo = iisManagerService.GetWebSiteInfo(searchSiteName, ip);

            foreach (var item in siteInfo)
            {
                item.State = keyValuePairs[item.State];
            }

            return new JsonResult(siteInfo);
        }
        /// <summary>
        /// 获取回滚包列表
        /// </summary>
        /// <param name="id">站点id</param>
        /// <returns></returns>
        [HttpGet("rollbackpackages/{id}")]
        public List<string> GetRollBackPackages([FromRoute] string id)
        {
            return iisManagerService.GetRollBackPackages(id);
        }
        /// <summary>
        /// 发布
        /// </summary>

        /// <param name="publishInfo">站点信息</param>
        [HttpPost("publish")]
        public async Task<ExcuteResponseInfo> Publish([FromForm] PublishInfo publishInfo)
        {
            return await iisManagerService.Publish(publishInfo);

        }
        /// <summary>
        /// 回滚
        /// </summary>
        /// <param name="rollbackInfo"></param>

        [HttpPost("rollback")]
        public async Task<ExcuteResponseInfo> RollBack([FromBody] RollbackInfo rollbackInfo)
        {
            return await iisManagerService.RollBack(rollbackInfo);
        }
        /// <summary>
        /// 创建站点
        /// </summary>
        /// <param name="createSiteInfo">站点信息</param>
        [HttpPost("createsite")]
        public async Task<ExcuteResponseInfo> CreateSite([FromForm] CreateSiteInfo createSiteInfo)
        {

            return await iisManagerService.CreateSite(createSiteInfo);
        }
        /// <summary>
        /// 删除站点
        /// </summary>
        /// <param name="siteDeletedInfo"></param>
        /// <returns></returns>
        [HttpPost("delete")]
        public async Task<ExcuteResponseInfo> DeleteSite([FromBody] SiteDeletedInfo siteDeletedInfo)
        {

            return await  iisManagerService.DeleteSite(siteDeletedInfo);
        }
    }
}
