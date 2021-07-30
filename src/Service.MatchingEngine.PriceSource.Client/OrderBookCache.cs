using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.DataReader;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public class OrderBookCache : IOrderBookService
    {
        private readonly MyNoSqlReadRepository<OrderBookNoSql> _reader;

        public OrderBookCache(MyNoSqlReadRepository<OrderBookNoSql> reader)
        {
            _reader = reader;
        }

        public List<OrderBookLevelNoSql> GetOrderBook(string brokerId, string symbol)
        {
            var orderBook = _reader.Get(OrderBookNoSql.GeneratePartitionKey(brokerId, symbol));
            return orderBook.Select(e => e.Level).ToList();
        }
    }
}