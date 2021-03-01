using System;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.Domain.ServiceBus.PublisherSubscriber.BidAsks;
using MyJetWallet.MatchingEngine.Grpc;
using MyJetWallet.MatchingEngine.Grpc.Api;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using MyServiceBus.TcpClient;
using Service.MatchingEngine.EventBridge.ServiceBus;
using Service.MatchingEngine.PriceSource.Jobs;
using Service.MatchingEngine.PriceSource.MyNoSql;
using Service.MatchingEngine.PriceSource.Services;

namespace Service.MatchingEngine.PriceSource.Modules
{
    public class ServiceModule: Module
    {
        private static ILogger ServiceBusLogger { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<QuotePublisher>()
                .As<IQuotePublisher>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            ServiceBusLogger = Program.LogFactory.CreateLogger(nameof(MyServiceBusTcpClient));

            var serviceBusClient = new MyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName);
            serviceBusClient.PlugPacketHandleExceptions(ex => ServiceBusLogger.LogError(ex as Exception, "Exception in MyServiceBusTcpClient"));
            serviceBusClient.PlugSocketLogs((context, msg) => ServiceBusLogger.LogInformation($"MyServiceBusTcpClient[Socket {context?.Id}|{context?.Connected}|{context?.Inited}] {msg}"));
            builder.RegisterInstance(serviceBusClient).AsSelf().SingleInstance();
            
            builder.RegisterMeEventSubscriber(serviceBusClient, "price-source-1", false);

            builder.RegisterType<OutgoingEventJob>().AutoActivate().SingleInstance();

            builder
                .RegisterType<OrderBookAggregator>()
                .As<IOrderBookAggregator>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<TradeVolumeAggregator>()
                .As<ITradeVolumeAggregator>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterMatchingEngineGrpcClient();

            RegisterMyNoSqlWriter<OrderBookNoSql>(builder, OrderBookNoSql.TableName);

            builder.RegisterMatchingEngineGrpcClient(orderBookServiceGrpcUrl: Program.Settings.OrderBookServiceGrpcUrl);

            builder.RegisterBidAskPublisher(serviceBusClient);
            builder.RegisterTradeVolumePublisher(serviceBusClient);


            builder.Register(ctx => new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<BidAskNoSql>(
                    Program.ReloadedSettings(model => model.MyNoSqlWriterUrl), BidAskNoSql.TableName, true))
                .As<IMyNoSqlServerDataWriter<BidAskNoSql>>()
                .SingleInstance();

        }

        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx => new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<TEntity>(
                    Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}