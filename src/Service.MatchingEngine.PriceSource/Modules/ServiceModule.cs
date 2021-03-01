using System;
using Autofac;
using Microsoft.Extensions.Logging;
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
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<TradeVolumeAggregator>()
                .As<ITradeVolumeAggregator>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            RegisterMyNoSqlWriter<OrderBookNoSql>(builder, OrderBookNoSql.TableName);
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