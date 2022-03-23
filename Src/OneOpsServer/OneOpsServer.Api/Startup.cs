using CSRedis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OneOpsServer.Api.Model;
using OneOpsServer.Api.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneOpsServer.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var csredis = new CSRedisClient(configuration.GetConnectionString("Redis"));
            RedisHelper.Initialization(csredis);
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("OneopsServer");
            services.AddTransient<HttpWebClient>();
            var oneopsSetting = Configuration.GetSection("OneopsSetting").Get<OneopsSetting>();
            services.AddSingleton(oneopsSetting);
            services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OneOps", Version = "v1" });
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, "OneOpsServer.Api.xml");
                    options.IncludeXmlComments(xmlPath, true);
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                    {
                        Description = "在下框中输入请求头中需要添加Jwt授权Token：Bearer Token",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        BearerFormat = "JWT",
                        Scheme = "Bearer"
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                        {
                            new OpenApiSecurityScheme{
                                Reference = new OpenApiReference {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"}
                           },new string[] { }
                        }
                        });

                });
            services.AddHttpContextAccessor();
            services.AddScoped<IISManagerService>();
            //services.AddSingleton<RabbitMqClient, RabbitMqClient>();
            services.AddControllers();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "OneOps v1"); });
            app.UseStaticFiles();
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
