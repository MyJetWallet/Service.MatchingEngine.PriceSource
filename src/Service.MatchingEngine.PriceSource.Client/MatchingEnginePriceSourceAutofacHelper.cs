using Autofac;
using MyNoSqlServer.DataReader;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public static class MatchingEnginePriceSourceAutofacHelper
    {
        /// <summary>
        /// Register interfaces:
        ///   * ICurrentPricesCache
        /// </summary>
        public static void RegisterMatchingEnginePriceSourceClient(this ContainerBuilder builder, IMyNoSqlSubscriber myNoSqlSubscriber)
        {
            var subs = new MyNoSqlReadRepository<BidAskNoSql>(myNoSqlSubscriber, BidAskNoSql.TableName);
            builder
                .RegisterInstance(new CurrentPricesCache(subs))
                .As<ICurrentPricesCache>()
                .AutoActivate()
                .SingleInstance();
            
        }
    }
}