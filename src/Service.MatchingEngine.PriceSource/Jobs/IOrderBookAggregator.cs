using System.Collections.Generic;
using System.Threading.Tasks;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public interface IOrderBookAggregator
    {
        Task RegisterOrderUpdates(List<OrderBookOrder> updates);
    }
}