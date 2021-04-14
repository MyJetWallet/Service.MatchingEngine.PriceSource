using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public interface IDetailOrderBookService
    {
        DetailOrderBookNoSql GetOrderBook(string brokerId, string symbol);
    }
}