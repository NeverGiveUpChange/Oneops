
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.Model
{
    public class SiteInfo
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string State { get; set; }

        public string IP { get; set; }

        public string PhysicalPath { get; set; }
        public string ServerBindings { get; set; }
        public string CurrentVersion { get; set; }
    }
}
