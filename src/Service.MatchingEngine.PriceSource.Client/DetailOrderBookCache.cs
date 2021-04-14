using MyNoSqlServer.DataReader;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public class DetailOrderBookCache : IDetailOrderBookService
    {
        private readonly MyNoSqlReadRepository<DetailOrderBookNoSql> _reader;

        public DetailOrderBookCache(MyNoSqlReadRepository<DetailOrderBookNoSql> reader)
        {
            _reader = reader;
        }

        public DetailOrderBookNoSql GetOrderBook(string brokerId, string symbol)
        {
            var orderBook = _reader.Get(DetailOrderBookNoSql.GeneratePartitionKey(brokerId), DetailOrderBookNoSql.GenerateRowKey(symbol));
            return orderBook;
        }
    }
}