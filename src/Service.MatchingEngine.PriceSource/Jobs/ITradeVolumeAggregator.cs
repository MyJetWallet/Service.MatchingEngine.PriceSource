using System.Collections.Generic;
using System.Threading.Tasks;
using MyJetWallet.Domain.Prices;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public interface ITradeVolumeAggregator
    {
        Task RegisterTrades(List<TradeVolume> trades);
    }
}