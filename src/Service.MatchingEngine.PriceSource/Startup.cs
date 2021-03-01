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
        private MyServiceBusTcpClient _serviceBusTcpClient;

        public Startup()
        {
            _serviceBusTcpClient = new MyServiceBusTcpClient(() => Program.Settings.SpotServiceBusHostPort, ApplicationEnvironment.HostName);
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

            _serviceBusTcpClient.Start();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<SettingsModule>();
            builder.RegisterModule<ServiceModule>();

            builder
                .RegisterInstance(new BidAskMyServiceBusPublisher(_serviceBusTcpClient))
                .As<IPublisher<BidAsk>>()
                .SingleInstance();

            builder.Register(ctx => new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<BidAskNoSql>(
                    Program.ReloadedSettings(model => model.MyNoSqlWriterUrl), BidAskNoSql.TableName, true))
                .As<IMyNoSqlServerDataWriter<BidAskNoSql>>()
                .SingleInstance();
        }
    }
}
