using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOpsClient.Api.EF_Sqlite
{
    public class OneopsContext:DbContext
    {
        public OneopsContext(DbContextOptions<OneopsContext> Options) : base(Options)
        {
        }
        public DbSet<ServerInfo> ServerInfos { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }
    }
}
