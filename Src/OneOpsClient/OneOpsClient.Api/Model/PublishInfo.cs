using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class PublishInfo: BaseInfo
    {
        /// <summary>
        /// 待发布的项目文件
        /// </summary>

        public IFormFile FormFile { get; set; }
        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; set; }
        /// <summary>
        /// 发布的机器ip和站点id字典
        /// </summary>
        public string IpIdStr{ get; set; }
        public List<IpId> IpIds { get; set; }

        public int MaxVersion { get; set; }

    }
    public class IpId {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }



}
