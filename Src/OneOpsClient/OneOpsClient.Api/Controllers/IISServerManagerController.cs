using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OneOpsClient.Api.EF_Sqlite;
using OneOpsClient.Api.Model;
using OneOpsClient.Api.RabbitMqListener;
using OneOpsClient.Api.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OneOpsClient.Api.Controllers
{
    [Route("api/iisservermanager")]
    [ApiController]
    [Authorize]

    public class IISServerManagerController : ControllerBase
    {
        readonly OneopsContext oneopsContext;
        readonly IHttpContextAccessor httpContextAccessor;
        readonly HttpWebClient httpWebClient;
        static Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "1", "Starting" }, { "2", "Started" }, { "3", "Stopping" }, { "4", "Stopped" }, { "5", "Pausing" }, { "6", "Paused" }, { "7", "Continuing" } };
        string userName;
        string env;
        readonly OneopsSetting oneopsSetting;
        readonly SendMessage sendMessage;
        public IISServerManagerController(OneopsContext oneopsContext, IHttpContextAccessor httpContextAccessor, HttpWebClient httpWebClient, OneopsSetting oneopsSetting, SendMessage sendMessage)
        {
            this.oneopsContext = oneopsContext;
            this.httpContextAccessor = httpContextAccessor;
            this.httpWebClient = httpWebClient;
            this.oneopsSetting = oneopsSetting;

            userName = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Name))?.Value;
            env = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Role))?.Value;
            this.sendMessage = sendMessage;


        }
        /// <summary>
        /// 获取服务器站点信息
        /// </summary>
        /// <param name="searchIp"></param>
        /// <param name="searchSiteName"></param>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [HttpGet("websiteinfo")]
        public async Task<JsonResult> WebSiteInfoAsync([FromQuery] string searchIp, [FromQuery] string searchSiteName, [FromQuery] int page, [FromQuery] int limit)
        {
            List<SiteInfo> siteInfos = new List<SiteInfo>();
            var ipList = oneopsContext.ServerInfos.Where(x => !x.IsDelete && x.Env == env).Select(x => x.Ip).ToList();
            if (!string.IsNullOrWhiteSpace(searchIp))
            {
                ipList = ipList.Where(x => x == searchIp).ToList();
            }
            var taskList = new List<Task<string>>();
            foreach (var item in ipList)
            {
                //taskList.Add(
                //      httpWebClient.GetAsync($"https://localhost:44300/api/iisservermanager/websiteinfo?searchSiteName={searchSiteName}&ip={item}")

                taskList.Add(httpWebClient.GetAsync(string.Format(oneopsSetting.ApiAdress.WebSiteInfo, item, searchSiteName, item)));
                
            }
            Task.WaitAll(taskList.ToArray());
            foreach (var item in taskList)
            {

                var oneResult = JsonConvert.DeserializeObject<List<SiteInfo>>(item.Result);
                siteInfos = siteInfos.Union(oneResult).ToList();
            }
            var count = siteInfos.Count();
            siteInfos = siteInfos.Skip((page - 1) * limit).Take(limit).ToList();
            var result = new { Code = 0, Msg = "", Count = count, Data = siteInfos };
            return await Task.FromResult<JsonResult>(new JsonResult(result));
        }
        /// <summary>
        /// 回滚指定站点回滚包
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("rollbackpackages/{ip}/{id}")]
        public async Task<List<string>> RollBackPackagesAsync([FromRoute] string ip, [FromRoute] int id)
        {
            List<string> rollBackPackages = new List<string>();
            if (oneopsContext.ServerInfos.Any(x => !x.IsDelete && x.Ip == ip && x.Env == env))
            {

                //rollBackPackages = JsonConvert.DeserializeObject<List<string>>(await httpWebClient.GetAsync(string.Format("https://localhost:44300/api/iisservermanager/rollbackpackages/{0}", id)));
                rollBackPackages = JsonConvert.DeserializeObject<List<string>>(await httpWebClient.GetAsync(string.Format(oneopsSetting.ApiAdress.RollBackPackages, ip, id)));
            }
            return rollBackPackages;
        }
        /// <summary>
        /// 站点发布
        /// </summary>
        /// <param name="publishInfo"></param>
        [HttpPost("publish")]
        public async Task<JsonResult> Publish([FromForm] PublishInfo publishInfo)
        {

            if (publishInfo.FormFile == null)
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:文件必传", Status = false });



                return new JsonResult(new { IsSuccess = false, Message = $"文件必传" });
            }
            if (string.IsNullOrWhiteSpace(publishInfo.IpIdStr))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:Ip必传", Status = false });

                return new JsonResult(new { IsSuccess = false, Message = $"Ip必传" });
            }
            var fileNameShardArray = publishInfo.FormFile.FileName.Split('_');

            if (fileNameShardArray.Length <= 1)
            {

                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:文件名需xxx_n.zip", Status = false });

                return new JsonResult(new { IsSuccess = false, Message = $"文件名需xxx_n.zip" });

            }
            int version = 0;
            if (!int.TryParse(Path.GetFileNameWithoutExtension(fileNameShardArray.LastOrDefault()), out version))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:版本号非数字", Status = false });

                return new JsonResult(new { IsSuccess = false, Message = $"版本号非数字" });
            }
            if (version <= publishInfo.MaxVersion)
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:发布版本号小于项目最大版本号", Status = false });

                return new JsonResult(new { IsSuccess = false, Message = $"发布版本号小于项目最大版本号" });
            }
            publishInfo.IpIds = JsonConvert.DeserializeObject<List<IpId>>(publishInfo.IpIdStr);
            var allIpList = oneopsContext.ServerInfos.Where(x => !x.IsDelete).Select(x => x.Ip).ToList();
            publishInfo.IpIds = publishInfo.IpIds.Where(x => allIpList.Contains(x.Ip)).ToList();
            foreach (var item in publishInfo.IpIds)
            {
                var sendBody = new MultipartFormDataContent();
                sendBody.Add(new StringContent(publishInfo.SiteName), "SiteName");
                sendBody.Add(new StringContent(item.Id), "Id");
                sendBody.Add(new StringContent(userName), "UserName");
                sendBody.Add(new StringContent(item.Ip), "Ip");
                sendBody.Add(new StreamContent(publishInfo.FormFile.OpenReadStream()), "FormFile", publishInfo.FormFile.FileName);

                //var publishId = await httpWebClient.PostAsync(sendBody, $"https://localhost:44300/api/iisservermanager/publish");
                var excuteResponseInfo = JsonConvert.DeserializeObject<ExcuteResponseInfo>(await httpWebClient.PostAsync(sendBody, string.Format(oneopsSetting.ApiAdress.Publish, item.Ip)));
                if (excuteResponseInfo.Status)
                {
                    await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.EverytimeCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{item.Ip}服务器{publishInfo.SiteName}_发布完成_发布步骤Id:{excuteResponseInfo.StepId}", Status = true });

                }


            }

            await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.AllCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{string.Join(",", publishInfo.IpIds.Select(x => x.Ip).ToList()) }服务器{publishInfo.SiteName}应用程序_全部发布完成" ,Status=true});

            return new JsonResult(new { IsSuccess = false, Message = "发布成功" });
        }

        /// <summary>
        /// 回滚
        /// </summary>
        /// <param name="rollbackInfo"></param>
        [HttpPost("rollback")]
        public async Task<JsonResult> RollBackAsync([FromForm] RollbackInfo rollbackInfo)
        {

            if (string.IsNullOrWhiteSpace(rollbackInfo.Ip) && !oneopsContext.ServerInfos.Any(x => x.Ip == rollbackInfo.Ip && x.Env == env))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{env}环境无此Ip", Status = false });





                return new JsonResult(new { IsSuccess = false, Message = $"{env}环境无此Ip" });
            }
            rollbackInfo.UserName = userName;
            var excuteResponseInfo = JsonConvert.DeserializeObject<ExcuteResponseInfo>(await httpWebClient.PostAsync(JsonConvert.SerializeObject(rollbackInfo), string.Format(oneopsSetting.ApiAdress.RollBack, rollbackInfo.Ip)));
            //var rollbackId = await httpWebClient.PostAsync(JsonConvert.SerializeObject(rollbackInfo), " https://localhost:44300/api/iisservermanager/rollback");
            if (excuteResponseInfo.Status)
            {

                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.AllCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{ rollbackInfo.Ip}服务器{rollbackInfo.SiteName}应用程序_全部回滚完成_回滚步骤Id:{excuteResponseInfo.StepId}", Status = true });
            };




            return new JsonResult(new { IsSuccess = true, Message = "回滚成功" });
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="createSiteInfo"></param>
        [HttpPost("createsite")]
        public async Task<JsonResult> CreateSite([FromForm] CreateSiteInfo createSiteInfo)
        {
            if (string.IsNullOrWhiteSpace(createSiteInfo.Ips))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:Ip必传", Status = false });




                return new JsonResult(new { IsSuccess = false, Message = $"Ip必传" });
            }
            if (createSiteInfo.FormFile == null)
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:文件必传", Status = false });




                return new JsonResult(new { IsSuccess = false, Message = $"文件必传" });
            }
            var fileNameShardArray = createSiteInfo.FormFile.FileName.Split('_');

            if (fileNameShardArray.Length <= 1)
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:文件名需xxx_n.zip", Status = false });



                return new JsonResult(new { IsSuccess = false, Message = $"文件名需xxx_n.zip" });
            }
            int version = 0;
            if (!int.TryParse(Path.GetFileNameWithoutExtension(fileNameShardArray.LastOrDefault()), out version))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:版本号非数字", Status = false });





                return new JsonResult(new { IsSuccess = false, Message = $"版本号非数字" });
            }
            if (string.IsNullOrWhiteSpace(createSiteInfo.PoolName))
            {
                createSiteInfo.PoolName = createSiteInfo.SiteName;
            }
            createSiteInfo.IPList = createSiteInfo.Ips.Split(',').ToList();
            createSiteInfo.IPList = oneopsContext.ServerInfos.Where(x => !x.IsDelete && x.Env == env && createSiteInfo.IPList.Contains(x.Ip)).Select(x => x.Ip).ToList();

            foreach (var item in createSiteInfo.IPList)
            {
                var sendBody = new MultipartFormDataContent();
                sendBody.Add(new StringContent(createSiteInfo.SiteName), "SiteName");
                sendBody.Add(new StringContent(item), "Ip");
                sendBody.Add(new StringContent(createSiteInfo.Port), "Port");
                sendBody.Add(new StringContent(createSiteInfo.PoolName), "PoolName");
                sendBody.Add(new StringContent(createSiteInfo.RealmName), "RealmName");
                sendBody.Add(new StringContent(createSiteInfo.PublishDirectoryPath), "PublishDirectoryPath");
                sendBody.Add(new StringContent(userName), "UserName");
                sendBody.Add(new StringContent(createSiteInfo.ApplicationType.ToString()), "ApplicationType");
                sendBody.Add(new StreamContent(createSiteInfo.FormFile.OpenReadStream()), "FormFile", createSiteInfo.FormFile.FileName);
                //"http://{0}:9001/api/iisservermanager/createsite"
                //var excuteResponseInfo = JsonConvert.DeserializeObject<ExcuteResponseInfo>(await httpWebClient.PostAsync(sendBody, string.Format(oneopsSetting.ApiAdress.CreateSite, item)));
                var excuteResponseInfo = await httpWebClient.PostAsync(sendBody, string.Format(oneopsSetting.ApiAdress.CreateSite, item));
                //var createId = httpWebClient.PostAsync(sendBody, "https://localhost:44300/api/iisservermanager/createsite").Result;
                //if (excuteResponseInfo.Status)
                //{

                //    await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.EverytimeCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{item}服务器{createSiteInfo.SiteName}应用程序_创建完成_创建步骤Id:{excuteResponseInfo.StepId}" });
                //}



            }

            await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.AllCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{string.Join(",", createSiteInfo.IPList) }服务器{createSiteInfo.SiteName}应用程序_全部创建完成",Status=true });

            return new JsonResult(new { IsSuccess = true, Message = "创建成功" });
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="siteDeletedInfo"></param>
        [HttpPost("delete")]
        public async Task<JsonResult> DeleteSite([FromBody] SiteDeletedInfo siteDeletedInfo)
        {
            if (string.IsNullOrWhiteSpace(siteDeletedInfo.Ip) && !oneopsContext.ServerInfos.Any(x => x.Ip == siteDeletedInfo.Ip && x.Env == env))
            {
                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{env}环境无此Ip", Status = false });

                return new JsonResult(new { IsSuccess = false, Message = $"{env}环境无此Ip" });
            }
            siteDeletedInfo.UserName = userName;

            //var excuteResponseInfo = JsonConvert.DeserializeObject< ExcuteResponseInfo > (await httpWebClient.PostAsync(JsonConvert.SerializeObject(siteDeletedInfo), "https://localhost:44300/api/iisservermanager/delete"));
            var excuteResponseInfo = JsonConvert.DeserializeObject<ExcuteResponseInfo>(await httpWebClient.PostAsync(JsonConvert.SerializeObject(siteDeletedInfo), string.Format(oneopsSetting.ApiAdress.Delete, siteDeletedInfo.Ip)));

            if (excuteResponseInfo.Status)
            {


                await sendMessage.SendSiteMessage(new MessageModel { MethodName = oneopsSetting.SignalRSetting.AllCompleteMethodName, UserName = userName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{ siteDeletedInfo.Ip}服务器{siteDeletedInfo.SiteName}应用程序_删除完成_删除步骤Id:{excuteResponseInfo.StepId}" });
            }

            return new JsonResult(new { IsSuccess = true, Message = "删除成功" });
        }
    }
}
