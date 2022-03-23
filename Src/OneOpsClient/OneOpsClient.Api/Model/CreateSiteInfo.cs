using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class CreateSiteInfo:BaseInfo
    {
        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; set; }
        /// <summary>
        /// 端口号
        /// </summary>
        public string Port { get; set; }
        /// <summary>
        /// 程序池名称（默认和程序集名称相同可不传）
        /// </summary>
        public string PoolName { get; set; }

        public string Ips { get; set; }
        /// <summary>
        /// 要发布的机器ip列表
        /// </summary>
     

        public List<string> IPList { get; set; }
        /// <summary>
        /// 域名
        /// </summary>
        public string RealmName { get; set; }
        /// <summary>
        /// 发布文件夹路径
        /// </summary>
        public string PublishDirectoryPath { get; set; }

        public IFormFile FormFile { get; set; }

        public int ApplicationType { get; set; }


    }

}
