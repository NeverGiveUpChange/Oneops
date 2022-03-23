using CSRedis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OneOpsClient.Api.EF_Sqlite;
using OneOpsClient.Api.RabbitMqListener;
using OneOpsClient.Api.Redis;
using OneOpsClient.Api.RedisQueue;
using OneOpsClient.Api.SignalR;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OneOpsClient.Api
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

            var oneopsSetting = Configuration.GetSection("OneopsSetting").Get<OneopsSetting>();
            services.AddSingleton(oneopsSetting);
            services.AddHttpClient("OneopsClient");
            services.AddTransient<HttpWebClient>();

            services.AddDbContext<OneopsContext>(options => options.UseSqlite(Configuration.GetConnectionString("sqlite")));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
            {

                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(oneopsSetting.JwtSetting.TokenSecret)),
                    ValidIssuer = oneopsSetting.JwtSetting.ValidIssuer,
                    ValidAudience = oneopsSetting.JwtSetting.ValidAudience,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        //var userName = context.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Name))?.Value;
                        var accessToken = context.Request.Query["userName"].ToString();
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                      (path.StartsWithSegments("/sitemessage")))
                        {
                            // Read the token out of the query string
                            context.HttpContext.Request.Headers["userName"] = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });


            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OneOps", Version = "v1" });
                var xmlPath = Path.Combine(AppContext.BaseDirectory, "OneOpsClient.Api.xml");
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
            services.AddSingleton<SiteMessageHub>();
            //services.AddHostedService<RabbitCreateListener>();
            //services.AddHostedService<RabbitPublishListener>();
            //services.AddHostedService<RabbitRollbackListener>();
            //services.AddSingleton<RabbitMqClient>();
            services.AddSingleton<SendMessage>();

            services.AddSignalR(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(oneopsSetting.SignalRSetting.ClientTimeoutInterval);
                options.KeepAliveInterval = TimeSpan.FromMinutes(oneopsSetting.SignalRSetting.KeepAliveInterval);

            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAuthentication();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "OneOps v1"); });
            app.UseStaticFiles();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SiteMessageHub>("/sitemessage");
            });
        }
    }
}
