using System.Collections.Generic;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public interface IOrderBookService
    {
        List<OrderBookLevelNoSql> GetOrderBook(string brokerId, string symbol);
    }
}