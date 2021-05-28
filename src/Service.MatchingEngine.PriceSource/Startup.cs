using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Domain.ServiceBus.PublisherSubscriber.BidAsks;
using MyJetWallet.Sdk.GrpcMetrics;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using MyServiceBus.TcpClient;
using Prometheus;
using ProtoBuf.Grpc.Server;
using Service.MatchingEngine.PriceSource.Jobs;
using Service.MatchingEngine.PriceSource.Modules;
using Service.MatchingEngine.PriceSource.MyNoSql;
using Service.MatchingEngine.PriceSource.Services;
using SimpleTrading.BaseMetrics;
using SimpleTrading.ServiceStatusReporterConnector;

namespace Service.MatchingEngine.PriceSource
{
    public class Startup
    {
        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCodeFirstGrpc(options =>
            {
                options.Interceptors.Add<PrometheusMetricsInterceptor>();
                options.BindMetricsInterceptors();
            });

            services.AddHostedService<ApplicationLifetimeManager>();
            //services.AddHostedService<QuoteSimulator>();

            services.AddMyTelemetry("SP-", Program.Settings.ZipkinUrl);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseMetricServer();

            app.BindServicesTree(Assembly.GetExecutingAssembly());

            app.BindIsAlive();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<SettingsModule>();
            builder.RegisterModule<ServiceModule>();
        }
    }
}
