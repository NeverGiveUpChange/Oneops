using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OneOpsServer.Api.Model;
using OneOpsServer.Api.RedisQueue;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsServer.Api.Service
{
    public class IISManagerService
    {
        string iisPathFormat = "IIS://{0}/W3SVC";
        static string schemaClassName = "IIsWebServer";
        static string serverComment = "ServerComment";
        static string serverState = "ServerState";
        static string childrenSchemaClassName = "IIsWebVirtualDir";
        static string siteIdIncrementFormat = "{0}_siteIdIncrement";
        readonly OneopsSetting oneopsSetting;
        readonly HttpWebClient httpWebClient;
        //readonly RabbitMqClient rabbitMqClient;
        public IISManagerService(OneopsSetting oneopsSetting, HttpWebClient httpWebClient)
        {
            this.oneopsSetting = oneopsSetting;
            //this.rabbitMqClient = rabbitMqClient;
            this.httpWebClient = httpWebClient;

        }

        public List<SiteInfo> GetWebSiteInfo(string searchSiteName, string ip)
        {

            List<SiteInfo> siteInfoList = new List<SiteInfo>();

            DirectoryEntry directoryEntry = new DirectoryEntry(string.Format(iisPathFormat, "localhost"));

            foreach (DirectoryEntry item in directoryEntry.Children)
            {
                if (item.SchemaClassName == schemaClassName)
                {
                    SiteInfo siteInfo = new SiteInfo();
                    siteInfo.Name = item.Properties[serverComment].Value == null ? string.Empty : item.Properties[serverComment].Value.ToString();
                    if (!string.IsNullOrWhiteSpace(searchSiteName))
                    {
                        if (searchSiteName != siteInfo.Name)
                        {
                            continue;
                        }
                    }
                    siteInfo.Id = item.Name;
                    siteInfo.IP = ip;
                    siteInfo.State = item.Properties[serverState].Value.ToString();
                    siteInfo.ServerBindings = item.Properties["ServerBindings"].Value == null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(item.Properties["ServerBindings"].Value);
                    foreach (DirectoryEntry item1 in item.Children)
                    {
                        if ((item1.SchemaClassName == childrenSchemaClassName) && (item1.Name.ToLower() == "root"))
                        {
                            if (item1.Properties["Path"].Value != null)
                            {
                                siteInfo.PhysicalPath = item1.Properties["Path"].Value.ToString();
                                var fileNameShardArray = Path.GetFileNameWithoutExtension(siteInfo.PhysicalPath).Split("_");
                                siteInfo.CurrentVersion = fileNameShardArray.Length <= 1 ? "1" : fileNameShardArray.LastOrDefault();
                                break;
                            }
                        }
                    }
                    siteInfoList.Add(siteInfo);
                }
            }


            return siteInfoList;
        }
        public async Task<ExcuteResponseInfo> CreateSite(CreateSiteInfo createSiteInfo)
        {
            var guid = Guid.NewGuid().ToString();
            ExcuteResponseInfo excuteResponseInfo = new ExcuteResponseInfo();
            excuteResponseInfo.StepId = guid;
            excuteResponseInfo.Ip = createSiteInfo.Ip;
            excuteResponseInfo.Status = true;
            try
            {



                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{createSiteInfo.Ip}服务器{createSiteInfo.SiteName}应用程序_创建开始_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);




                DirectoryEntry rootEntry = new DirectoryEntry(string.Format(iisPathFormat, "localhost"));
                List<int> idList = new List<int>();
                foreach (DirectoryEntry child in rootEntry.Children)
                {
                    if (child.SchemaClassName == schemaClassName)
                    {
                        var name = child.Properties[serverComment].Value == null ? string.Empty : child.Properties[serverComment].Value.ToString();
                        if (name == createSiteInfo.SiteName)
                        {
                            await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:改服务器已有相同名称站点_站点名称:{name}_创建步骤Id:{guid}", Status = false }, oneopsSetting.CreateMessgaeUrl);
                            excuteResponseInfo.Status = false;

                            return excuteResponseInfo;
                        }
                        idList.Add(Convert.ToInt32(child.Name.ToString()));
                    }
                }
                idList.Sort();
                var siteIdIncrementRedisKey = string.Format(siteIdIncrementFormat, createSiteInfo.Ip);
                RedisHelper.IncrBy(siteIdIncrementRedisKey);
                var siteId = idList.LastOrDefault() + RedisHelper.Get<int>(siteIdIncrementRedisKey);

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:创建应用程序Id_站点Id为:{siteId}_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);





                var publishZipPath = Path.Combine(createSiteInfo.PublishDirectoryPath, createSiteInfo.FormFile.FileName);
                if (!Directory.Exists(createSiteInfo.PublishDirectoryPath))
                {
                    Directory.CreateDirectory(createSiteInfo.PublishDirectoryPath);
                }
                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:创建应用程序发布路径_路径为:{createSiteInfo.PublishDirectoryPath}_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);






                using (var fs = File.Create(publishZipPath))
                {
                    createSiteInfo.FormFile.CopyTo(fs);
                }

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:保存压缩包_文件名:{ createSiteInfo.FormFile.FileName}_保存地址:{publishZipPath}_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);






                ZipFile.ExtractToDirectory(publishZipPath, createSiteInfo.PublishDirectoryPath, true);


                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:解压压缩包__解压地址:{createSiteInfo.PublishDirectoryPath}_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);







                File.Delete(publishZipPath);

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:删除压缩包_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);





                var publishPath = Path.Combine(createSiteInfo.PublishDirectoryPath, Path.GetFileNameWithoutExtension(publishZipPath));
                CreatPool(createSiteInfo.PoolName, createSiteInfo.ApplicationType);

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:创建应用程序池_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);





                DirectoryEntry site = (DirectoryEntry)rootEntry.Invoke("Create", new Object[] { schemaClassName, siteId });

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:创建应用程序_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);






                site.Invoke("Put", serverComment, createSiteInfo.SiteName);
                site.Invoke("Put", "KeyType", schemaClassName);
                site.Invoke("Put", "ServerBindings", $"*:{createSiteInfo.Port}:{createSiteInfo.RealmName}");
                site.Invoke("Put", serverState, "1");
                site.Invoke("SetInfo");

                DirectoryEntry siteVDir = site.Children.Add("Root", childrenSchemaClassName);
                siteVDir.Properties["AppPoolId"].Value = createSiteInfo.PoolName;
                siteVDir.Properties["Path"][0] = publishPath;

                siteVDir.CommitChanges();
                site.CommitChanges();
                RedisHelper.IncrBy(siteIdIncrementRedisKey, -1);

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:设置应用程序:名称、域名、端口、状态、应用程序池，物理路径_创建步骤Id:{guid}" }, oneopsSetting.CreateMessgaeUrl);
            }
            catch (Exception ex) {
                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.CreateMethodName, UserName = createSiteInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:应用程序创建异常_异常信息:{ex.Message}_创建步骤Id:{guid}" ,Status=false}, oneopsSetting.CreateMessgaeUrl);
                excuteResponseInfo.Status = false;

            }



            return excuteResponseInfo;
        }
        public bool CreatPool(string appPoolName, int applicationType)
        {

            DirectoryEntry apppools = new DirectoryEntry("IIS://localhost/W3SVC/AppPools");
            DirectoryEntry newpool = apppools.Children.Add(appPoolName, "IIsApplicationPool");
            newpool.Properties["AppPoolCommand"].Value = "1";// 立即启动应用程序池.1:以勾选,2:未勾选  
            newpool.Properties["AppPoolState"].Value = "2";// 是否启动:2:启动,4:停止,XX:回收  
            newpool.Properties["ManagedPipelineMode"].Value = "0";// 托管管道模式.0:集成,1:经典  
            newpool.Properties["ManagedRuntimeVersion"].Value = applicationType == 1 ? "" : "V4.0";// "":无托管代码,V2.0: .NET Framework V2.0XXXX,V4.0: .NET Framework V4.0XXXX  
            newpool.CommitChanges();
            return true;
        }
        public async Task<ExcuteResponseInfo> Publish(PublishInfo publishInfo)
        {

            var redisLockKey = $"{publishInfo.Ip}_{publishInfo.Id}";

            var guid = Guid.NewGuid().ToString();

            ExcuteResponseInfo excuteResponseInfo = new ExcuteResponseInfo();
            excuteResponseInfo.StepId = guid;
            excuteResponseInfo.Ip = publishInfo.Ip;
            excuteResponseInfo.Status = true;
            try
            {


                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{publishInfo.Ip}服务器{publishInfo.SiteName}应用程序_开始发布_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);






                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:应用程序_尝试加锁_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);





                if (RedisHelper.Set(redisLockKey, 1, 300, CSRedis.RedisExistence.Nx))
                {
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:加锁成功_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);



                    DirectoryEntry directoryEntry = new DirectoryEntry($"IIS://localhost/W3SVC/{publishInfo.Id}");

                    #region 获取当前站点信息
                    DirectoryEntry CurrentDirectoryEntry = _getCurrentDirectoryEntry(directoryEntry);
                    if (CurrentDirectoryEntry == null)
                    {
                        await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:未获取到当前应用程序信息_发布步骤Id:{guid}", Status = false }, oneopsSetting.PublishMessgaeUrl);
                        excuteResponseInfo.Status = false;
                        RedisHelper.Del(redisLockKey);
                        return excuteResponseInfo;
                    }

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:获取当前应用程序信息_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);





                    #endregion
                    #region 设置站点所需各文件路径
                    var sitePathInfo = _setSitePathInfo(CurrentDirectoryEntry, publishInfo.FormFile.FileName);
                    #endregion
                    #region 保存上传文件
                    using (var fs = System.IO.File.Create(sitePathInfo.PublishZipPath))
                    {
                        publishInfo.FormFile.CopyTo(fs);
                    }


                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:保存压缩包_文件名:{ publishInfo.FormFile.FileName}_保存地址:{sitePathInfo.PublishZipPath}_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);







                    #endregion
                    #region 解压并删除上传文件
                    ZipFile.ExtractToDirectory(sitePathInfo.PublishZipPath, sitePathInfo.CurrentSiteParentPath, true);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:解压压缩包__解压地址:{sitePathInfo.CurrentSiteParentPath}_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);







                    File.Delete(sitePathInfo.PublishZipPath);



                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:删除压缩包_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);







                    #endregion
                    #region 停止当前站点
                    directoryEntry.Invoke("Stop", new object[] { });


                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:停止应用程序_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);





                    #endregion
                    #region 备份
                    var rollBackPackageZipPath = _backup(sitePathInfo, directoryEntry.Properties[serverComment].Value.ToString(), publishInfo.UserName, guid);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:备份文件_文件地址:{rollBackPackageZipPath}_发布Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);



                    #endregion
                    #region 设置站点物理地址
                    CurrentDirectoryEntry.Properties["Path"][0] = sitePathInfo.PublishPath;
                    CurrentDirectoryEntry.CommitChanges();
                    directoryEntry.CommitChanges();

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:更改应用程序对应物理路径_路径为:{sitePathInfo.PublishPath}_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);




                    #endregion
                    #region 启动站点
                    directoryEntry.Invoke("Start", new object[] { });

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:启动应用程序_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);





                    #endregion
                    #region 删除原项目文件
                    Directory.Delete(sitePathInfo.CurrentSitePath, true);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:删除应用程序原包_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);






                    #endregion
                    #region 解锁
                    RedisHelper.Del(redisLockKey);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:解锁应用程序_发布步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);
                    #endregion

                }
                else
                {

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{publishInfo.Ip}服务器{publishInfo.SiteName}应用程序_加锁失败请稍后_发布步骤Id:{guid}", Status = false }, oneopsSetting.PublishMessgaeUrl);
                    excuteResponseInfo.Status = false;




                }
            }
            catch (Exception ex)
            {
                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.PublishMethodName, UserName = publishInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{publishInfo.Ip}服务器{publishInfo.SiteName}应用程序发布异常_异常信息:{ex.Message}_发布步骤Id:{guid}", Status = false }, oneopsSetting.PublishMessgaeUrl);
                excuteResponseInfo.Status = false;
                RedisHelper.Del(redisLockKey);
            }
            return excuteResponseInfo;
        }
        public async Task<ExcuteResponseInfo> RollBack(RollbackInfo rollbackInfo)
        {
            var guid = Guid.NewGuid().ToString();
            var redisLockKey = $"{rollbackInfo.Ip}_{rollbackInfo.Id}";
            ExcuteResponseInfo excuteResponseInfo = new ExcuteResponseInfo();
            excuteResponseInfo.StepId = guid;
            excuteResponseInfo.Ip = rollbackInfo.Ip;
            excuteResponseInfo.Status = true;
            try
            {


                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{rollbackInfo.Ip}服务器{rollbackInfo.SiteName}应用程序_开始回滚_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);



                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:应用程序_尝试加锁_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);


                if (RedisHelper.Set(redisLockKey, 1, 300, CSRedis.RedisExistence.Nx))
                {
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:应用程序_加锁成功_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);


                    DirectoryEntry directoryEntry = new DirectoryEntry($"IIS://localhost/W3SVC/{rollbackInfo.Id}");
                    #region 获取当前站点信息
                    DirectoryEntry CurrentDirectoryEntry = _getCurrentDirectoryEntry(directoryEntry);
                    if (CurrentDirectoryEntry == null)
                    {
                        await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:未获取到当前应用程序信息_回滚步骤Id:{guid}" ,Status=false}, oneopsSetting.RollbackMessageUrl);
                        excuteResponseInfo.Status = false;
                        RedisHelper.Del(redisLockKey);
                        return excuteResponseInfo;
                    }
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:获取当前应用程序信息_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);

                    #endregion
                    #region 获取备份文件
                    var currentSitePath = CurrentDirectoryEntry.Properties["Path"].Value.ToString();
                    DirectoryInfo pathInfo = new DirectoryInfo(currentSitePath);
                    var currentSiteDirectoryParentPath = pathInfo.Parent.FullName;
                    var siteName = directoryEntry.Properties[serverComment].Value;
                    var rollbackZipPath = Path.Combine(currentSiteDirectoryParentPath, $"{siteName}_RollBackPackages", rollbackInfo.FileName);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:获取应用程序回滚包_回滚包路径为:{rollbackZipPath}_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);



                    #endregion
                    #region 解压到发布路径
                    var publishPath = Path.Combine(currentSiteDirectoryParentPath, Path.GetFileNameWithoutExtension(rollbackZipPath));
                    ZipFile.ExtractToDirectory(rollbackZipPath, publishPath, true);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:解压应用程序回滚包到发布路径_发布路径为:{publishPath}_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);


                    #endregion
                    #region 删除回滚包
                    File.Delete(rollbackZipPath);


                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:删除应用程序回滚包_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);


                    #endregion
                    #region 停止当前站点
                    directoryEntry.Invoke("Stop", new object[] { });


                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:停止应用程序_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);


                    #endregion
                    #region 设置站点物理地址

                    CurrentDirectoryEntry.Properties["Path"][0] = publishPath;
                    CurrentDirectoryEntry.CommitChanges();

                    directoryEntry.CommitChanges();

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:更改应用程序对应物理路径_路径为:{publishPath}_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);





                    #endregion
                    #region 启动站点
                    directoryEntry.Invoke("Start", new object[] { });
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:启动应用程序_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);




                    #endregion
                    #region 删除原项目文件
                    Directory.Delete(currentSitePath, true);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:删除应用程序原包_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);



                    #endregion
                    #region 删除锁
                    RedisHelper.Del(redisLockKey);

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:解锁应用程序_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);



                    #endregion
                }
                else
                {
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{rollbackInfo.Ip}服务器{rollbackInfo.SiteName}应用程序_加锁失败请稍后_回滚步骤Id:{guid}" }, oneopsSetting.RollbackMessageUrl);
                    excuteResponseInfo.Status = false;


                }
            }
            catch (Exception ex) {
                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.RollBackMethodName, UserName = rollbackInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{rollbackInfo.Ip}服务器{rollbackInfo.SiteName}应用程序回滚异常_异常信息:{ex.Message}_回滚步骤Id:{guid}",Status=false }, oneopsSetting.RollbackMessageUrl);
                excuteResponseInfo.Status = false;
                RedisHelper.Del(redisLockKey);
            }
            return excuteResponseInfo;
        }

        public async Task<ExcuteResponseInfo> DeleteSite(SiteDeletedInfo siteDeletedInfo)
        {

            var guid = Guid.NewGuid().ToString();
            var redisLockKey = $"{siteDeletedInfo.Ip}_{siteDeletedInfo.Id}";
            ExcuteResponseInfo excuteResponseInfo = new ExcuteResponseInfo();
            excuteResponseInfo.StepId = guid;
            excuteResponseInfo.Ip = siteDeletedInfo.Ip;
            excuteResponseInfo.Status = true;
            try
            {

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{siteDeletedInfo.Ip}服务器{siteDeletedInfo.SiteName}应用程序_删除开始_删除步骤Id:{guid}" }, oneopsSetting.DeleteMessageUrl);


                //RedisHelper.Del(redisLockKey);

                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:应用程序_尝试加锁_删除步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);
                if (RedisHelper.Set(redisLockKey, 1, 300, CSRedis.RedisExistence.Nx))
                {
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:加锁成功_删除步骤Id:{guid}" }, oneopsSetting.DeleteMessageUrl);


                    DirectoryEntry directoryEntry = new DirectoryEntry($"IIS://localhost/W3SVC/{siteDeletedInfo.Id}");

                    #region 获取当前站点信息
                    DirectoryEntry CurrentDirectoryEntry = _getCurrentDirectoryEntry(directoryEntry);
                    if (CurrentDirectoryEntry == null)
                    {
                        await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:未获取到当前应用程序信息_删除步骤Id:{guid}",Status=false }, oneopsSetting.DeleteMessageUrl);
                        excuteResponseInfo.Status = false;
                        RedisHelper.Del(redisLockKey);
                        return excuteResponseInfo;
                    }

                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:获取当前应用程序信息_删除步骤Id:{guid}" }, oneopsSetting.DeleteMessageUrl);

                    var sitePath = CurrentDirectoryEntry.Properties["Path"].Value.ToString();

                    var directoryInfo = new DirectoryInfo(sitePath);
                    var parenDdirectoryPath = directoryInfo.Parent.FullName;
                    var appPoolName = CurrentDirectoryEntry.Properties["AppPoolId"].Value.ToString();
                    DirectoryEntry appPoolEntry = new DirectoryEntry($"IIS://localhost/W3SVC/AppPools");

                    foreach (DirectoryEntry item in appPoolEntry.Children)
                    {
                        if (item.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase))
                        {


                            CurrentDirectoryEntry.CommitChanges();
                            directoryEntry.CommitChanges();

                            break;
                        }
                    }
                    var deleteDirectoryInfo = new DirectoryInfo(parenDdirectoryPath);
                    if (deleteDirectoryInfo.Exists)
                    {
                        deleteDirectoryInfo.Delete(true);
                    }

                    #endregion


                }
                else
                {
                    await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{siteDeletedInfo.Ip}服务器{siteDeletedInfo.SiteName}应用程序_加锁失败请稍后_删除步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);
                    excuteResponseInfo.Status = false;

                }
            }
            catch (Exception ex) {
                await httpWebClient.PostAsync(new MessageModel { MethodName = oneopsSetting.SignalRSetting.DeleteMethodName, UserName = siteDeletedInfo.UserName, StepMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}:{siteDeletedInfo.Ip}服务器{siteDeletedInfo.SiteName}应用程序_删除异常_异常信息:{ex.Message}_删除步骤Id:{guid}" }, oneopsSetting.PublishMessgaeUrl);
                excuteResponseInfo.Status = false;
            }

            return excuteResponseInfo;

        }
        public List<string> GetRollBackPackages(string id)
        {
            List<string> rollBackPackages = new List<string>();
            DirectoryEntry directoryEntry = new DirectoryEntry($"IIS://localhost/W3SVC/{id}");
            var currentSitePath = string.Empty;
            foreach (DirectoryEntry item1 in directoryEntry.Children)
            {
                if ((item1.SchemaClassName == childrenSchemaClassName) && (item1.Name.ToLower() == "root"))
                {
                    if (item1.Properties["Path"].Value != null)
                    {
                        currentSitePath = item1.Properties["Path"].Value.ToString();
                        break;
                    }
                }
            }
            DirectoryInfo pathInfo = new DirectoryInfo(currentSitePath);
            var parentPath = pathInfo.Parent.FullName;
            var rollbackDirectoryPath = Path.Combine(parentPath, $"{ directoryEntry.Properties[serverComment].Value}_RollBackPackages");
            DirectoryInfo directoryInfo = new DirectoryInfo(rollbackDirectoryPath);
            if (directoryInfo.Exists)
            {
                rollBackPackages = directoryInfo.GetFiles().Where(x => x.Extension == ".zip").Select(x => x.Name).ToList();
            }
            return rollBackPackages;
        }
        private DirectoryEntry _getCurrentDirectoryEntry(DirectoryEntry parentDirectoryEntry)
        {
            DirectoryEntry CurrentDirectoryEntry = null;
            foreach (DirectoryEntry item1 in parentDirectoryEntry.Children)
            {
                if ((item1.SchemaClassName == childrenSchemaClassName) && (item1.Name.ToLower() == "root"))
                {
                    CurrentDirectoryEntry = item1;
                    break;

                }
            }
            return CurrentDirectoryEntry;
        }

        private SitePathInfo _setSitePathInfo(DirectoryEntry directoryEntry, string fileName)
        {
            SitePathInfo sitePathInfo = new SitePathInfo { };
            sitePathInfo.CurrentSitePath = directoryEntry.Properties["Path"].Value.ToString();
            DirectoryInfo currentSiteDirectoryInfo = new DirectoryInfo(sitePathInfo.CurrentSitePath);//获取当前站点文件信息
            sitePathInfo.CurrentSiteParentPath = currentSiteDirectoryInfo.Parent.FullName; //获取当前站点文件父级目录路径
            sitePathInfo.PublishZipPath = Path.Combine(sitePathInfo.CurrentSiteParentPath, fileName);//新的发布文件路径
            sitePathInfo.PublishPath = Path.Combine(sitePathInfo.CurrentSiteParentPath, Path.GetFileNameWithoutExtension(sitePathInfo.PublishZipPath));//新的发布文件项目路径
            sitePathInfo.CurrentSiteFileName = currentSiteDirectoryInfo.Name;
            return sitePathInfo;
        }


        private string _backup(SitePathInfo sitePathInfo, string siteName, string userName, string guid)
        {
            var rollbackDirectoryPath = Path.Combine(sitePathInfo.CurrentSiteParentPath, $"{ siteName}_RollBackPackages");
            if (!Directory.Exists(rollbackDirectoryPath))
            {
                Directory.CreateDirectory(rollbackDirectoryPath);
            }
            DirectoryInfo rollbackDirectoryInfo = new DirectoryInfo(rollbackDirectoryPath);
            var files = rollbackDirectoryInfo.GetFiles().Where(x => x.Extension == ".zip").OrderBy(x => x.CreationTime);
            if (files != null && files.Count() > oneopsSetting.MaxRollbackPackagesCount)
            {
                files.LastOrDefault().Delete();
            }
            var rollBackPackageZipPath = Path.Combine(sitePathInfo.CurrentSiteParentPath, $"{siteName}_RollBackPackages", $"{sitePathInfo.CurrentSiteFileName}.zip");
            if (File.Exists(rollBackPackageZipPath))
            {
                File.Delete(rollBackPackageZipPath);
            }
            ZipFile.CreateFromDirectory(sitePathInfo.CurrentSitePath, rollBackPackageZipPath);
            return rollBackPackageZipPath;






        }
    }
}
