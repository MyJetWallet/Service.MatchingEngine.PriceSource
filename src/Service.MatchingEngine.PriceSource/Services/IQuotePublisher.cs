using System.Threading.Tasks;
using MyJetWallet.Domain.Prices;

namespace Service.MatchingEngine.PriceSource.Services
{
    public interface IQuotePublisher
    {
        Task Register(BidAsk quote);
    }
}