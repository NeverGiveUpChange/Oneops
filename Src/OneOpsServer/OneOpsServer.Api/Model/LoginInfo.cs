using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsServer.Api.Model
{
    public class LoginInfo
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string PassWord { get; set; }

        /// <summary>
        /// 环境
        /// </summary>
        public string Env { get; set; }
    }
}
