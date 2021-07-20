using System;
using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public class CurrentPricesCache : ICurrentPricesCache
    {
        private readonly MyNoSqlReadRepository<BidAskNoSql> _reader;

        public CurrentPricesCache(MyNoSqlReadRepository<BidAskNoSql> reader)
        {
            _reader = reader;
        }

        public BidAsk GetPrice(string brokerId, string symbol)
        {
            var price = _reader.Get(BidAskNoSql.GeneratePartitionKey(brokerId), BidAskNoSql.GenerateRowKey(symbol));
            return price?.Quote;
        }

        public List<BidAsk> GetPrices(string brokerId)
        {
            var list = _reader.Get(BidAskNoSql.GeneratePartitionKey(brokerId));
            return list.Select(e => e.Quote).ToList();
        }

        public List<BidAsk> GetPrices()
        {
            var list = _reader.Get();
            return list.Select(e => e.Quote).ToList();
        }

        public IMyNoSqlServerDataReader<BidAskNoSql> SubscribeToUpdateEvents(Action<IReadOnlyList<BidAskNoSql>> updateSubscriber, Action<IReadOnlyList<BidAskNoSql>> deleteSubscriber)
        {
            return _reader.SubscribeToUpdateEvents(updateSubscriber, deleteSubscriber);
        }
    }
}