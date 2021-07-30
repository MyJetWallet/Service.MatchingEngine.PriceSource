using System;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.MatchingEngine.Grpc;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.GrpcDataWriter;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.MatchingEngine.EventBridge.ServiceBus;
using Service.MatchingEngine.PriceSource.Jobs;
using Service.MatchingEngine.PriceSource.MyNoSql;
using Service.MatchingEngine.PriceSource.Services;
using SimpleTrading.Abstraction.BidAsk;
using SimpleTrading.ServiceBus.PublisherSubscriber.BidAsk;

namespace Service.MatchingEngine.PriceSource.Modules
{
    public class ServiceModule: Module
    {
        private static ILogger ServiceBusLogger { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);

            builder.RegisterMeEventSubscriber(serviceBusClient, "price-source-3", TopicQueueType.PermanentWithSingleConnection);

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


            MyNoSqlGrpcDataWriter noSqlWriter = MyNoSqlGrpcDataWriterFactory
                .CreateNoSsl(Program.Settings.MyNoSqlWriterGrpc)
                .RegisterSupportedEntity<OrderBookNoSql>(OrderBookNoSql.TableName)
                .RegisterSupportedEntity<BidAskNoSql>(BidAskNoSql.TableName);


            builder.RegisterInstance(noSqlWriter).AsSelf().SingleInstance();


            builder.RegisterMatchingEngineGrpcClient(orderBookServiceGrpcUrl: Program.Settings.OrderBookServiceGrpcUrl);

            builder.RegisterBidAskPublisher(serviceBusClient);
            builder.RegisterTradeVolumePublisher(serviceBusClient);

            RegisterCandlePublisher(builder);
        }

        private static void RegisterCandlePublisher(ContainerBuilder builder)
        {
            ServiceBusLogger = Program.LogFactory.CreateLogger(nameof(MyServiceBusTcpClient));

            var serviceBusClient = new MyServiceBusTcpClient(Program.ReloadedSettings(e => e.CandleServiceBusHostPort), ApplicationEnvironment.HostName);
            serviceBusClient.Log.AddLogException(ex => ServiceBusLogger.LogError(ex as Exception, "[CANDLE] Exception in MyServiceBusTcpClient"));
            serviceBusClient.Log.AddLogInfo(info => ServiceBusLogger.LogInformation($"[CANDLE] {info}"));
            serviceBusClient.SocketLogs.AddLogException((context, ex) => ServiceBusLogger.LogError(ex as Exception, $"[CANDLE] [Socket {context?.Id}|{context?.Inited}]Exception in MyServiceBusTcpClient on Socket level"));
            serviceBusClient.SocketLogs.AddLogInfo((context, info) => ServiceBusLogger.LogInformation($"[CANDLE] MyServiceBusTcpClient[Socket {context?.Id}|{context?.Inited}] {info}"));

            var candlePublisher = new BidAskMyServiceBusPublisher(serviceBusClient, "spot-bidask");

            builder
                .RegisterInstance(candlePublisher)
                .As<IPublisher<IBidAsk>>()
                .SingleInstance();

            serviceBusClient.Start();
        }
    }
}