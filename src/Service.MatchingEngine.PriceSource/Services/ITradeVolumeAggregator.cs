using System.Collections.Generic;
using System.Threading.Tasks;
using MyJetWallet.Domain.Prices;

namespace Service.MatchingEngine.PriceSource.Services
{
    public interface ITradeVolumeAggregator
    {
        Task RegisterTrades(List<TradeVolume> trades);
    }
}