using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsServer.Api.Model
{
    public class SitePathInfo
    {
        public string CurrentSitePath { get; set; }
        public string CurrentSiteParentPath { get; set; }

        public string PublishZipPath { get; set; }

        public string PublishPath { get; set; }

        public string CurrentSiteFileName { get; set; }
    }
}
