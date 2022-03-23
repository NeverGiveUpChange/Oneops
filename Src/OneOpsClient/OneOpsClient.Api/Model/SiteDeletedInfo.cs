using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class SiteDeletedInfo:BaseInfo
    {
        public string Ip { get; set; }
        public string Id { get; set; }

        public string SiteName { get; set; }
    }
}
