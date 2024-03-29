using AFL.Ordering.API.Extensions;
using AFL.Ordering.Infrastructure;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Prometheus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AFL.Ordering.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        internal static bool Ready { get; set; } = true;
        internal static bool Live { get; set; } = true;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region
            services.AddGrpc();

            services.AddHealthChecks()
                .AddMySql(Configuration.GetValue<string>("Mysql"), "mysql", tags: new string[] { "mysql", "live", "all" })
                .AddRabbitMQ(s =>
                {
                    var connectionFactory = new RabbitMQ.Client.ConnectionFactory();
                    Configuration.GetSection("RabbitMQ").Bind(connectionFactory);
                    return connectionFactory;
                }, "rabbitmq", tags: new string[] { "rabbitmq", "live", "all" })
                .AddCheck("liveChecker", () =>
                {
                    return Live ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
                }, new string[] { "live", "all" })
                .AddCheck("readyChecker", () =>
                {
                    return Ready ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
                }, new string[] { "ready", "all" });



            services.AddControllers().AddNewtonsoftJson(); //支持构造函数序列化
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddMediatRServices();
            services.AddMySqlDomainContext(Configuration.GetValue<string>("Mysql"));//数据库连接注入
            services.AddRepositories();
            services.AddEventBus(Configuration);
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                options.ForwardedHeaders = ForwardedHeaders.All;
            });
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionless();

            if (Configuration.GetValue("USE_PathBase", false))
            {
                app.Use((context, next) =>
                {
                    context.Request.PathBase = new PathString("/mobile");
                    return next();
                });
            }

            if (Configuration.GetValue("USE_Forwarded_Headers", false))
            {
                app.UseForwardedHeaders();
            }
            //app.UseHttpsRedirection();
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dc = scope.ServiceProvider.GetService<OrderingContext>();
                dc.Database.EnsureCreated();//确定数据库已创建数据表,如果没有就会创建数据库,如果数据库中存在相关的表，则需要手动更新数据库
            }


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapHealthChecks("/live", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") });
                endpoints.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
                endpoints.MapHealthChecks("/hc", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapControllers();
                //endpoints.MapGrpcService<AFL.Ordering.API.Grpc.OrderServiceImpl>();
            });
        }
    }
}
