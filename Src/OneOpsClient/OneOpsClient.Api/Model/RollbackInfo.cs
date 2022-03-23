using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class RollbackInfo: BaseInfo
    {
        /// <summary>
        ///  站点在当前服务器上的id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 站点所在服务器ip
        /// </summary>
        public string Ip { get; set; }
        /// <summary>
        /// 回滚包名称
        /// </summary>
        public string FileName { get; set; }

        public string SiteName { get; set; }

    }
}
