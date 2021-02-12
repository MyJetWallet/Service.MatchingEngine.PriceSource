using System.Collections.Generic;
using MyJetWallet.Domain.Prices;

namespace Service.MatchingEngine.PriceSource.Client
{
    public interface ICurrentPricesCache
    {
        BidAsk GetPrice(string brokerId, string symbol);
        List<BidAsk> GetPrices(string brokerId);
        List<BidAsk> GetPrices();
    }
}