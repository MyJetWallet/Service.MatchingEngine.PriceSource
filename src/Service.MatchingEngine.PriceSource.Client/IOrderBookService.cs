using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public interface IOrderBookService
    {
        OrderBookNoSql GetOrderBook(string brokerId, string symbol);
    }
}