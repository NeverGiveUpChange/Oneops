using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneOpsServer.Api.Model
{
    public class PublishInfo : BaseInfo
    {
        /// <summary>
        /// 待发布的项目文件
        /// </summary>

        public IFormFile FormFile { get; set; }
        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; set; }
        public string Id { get; set; }
        public string Ip { get; set; }

    }




}
