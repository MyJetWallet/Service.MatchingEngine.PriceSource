using System;
using System.Collections.Generic;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.Abstractions;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Client
{
    public interface ICurrentPricesCache
    {
        BidAsk GetPrice(string brokerId, string symbol);
        List<BidAsk> GetPrices(string brokerId);
        List<BidAsk> GetPrices();
        IMyNoSqlServerDataReader<BidAskNoSql> SubscribeToUpdateEvents(
            Action<IReadOnlyList<BidAskNoSql>> updateSubscriber, Action<IReadOnlyList<BidAskNoSql>> deleteSubscriber);
    }
}