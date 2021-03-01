using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain.Prices;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class TradeVolumeAggregator : ITradeVolumeAggregator, IStartable, IDisposable
    {
        private readonly IPublisher<TradeVolume> _publisher;

        public TradeVolumeAggregator(IPublisher<TradeVolume> publisher)
        {
            _publisher = publisher;
        }

        public Task RegisterTrades(List<TradeVolume> trades)
        {
            var taskList = new List<Task>();

            foreach (var trade in trades.Where(e => e.Volume < 0).OrderBy(e => e.Price))
            {
                var task = _publisher.PublishAsync(trade).AsTask();
                taskList.Add(task);
            }

            foreach (var trade in trades.Where(e => e.Volume > 0).OrderByDescending(e => e.Price))
            {
                var task = _publisher.PublishAsync(trade).AsTask();
                taskList.Add(task);
            }

            return Task.WhenAll(taskList);
        }

        public void Start()
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}