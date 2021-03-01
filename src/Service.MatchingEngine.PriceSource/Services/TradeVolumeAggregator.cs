using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Prices;
using Service.MatchingEngine.PriceSource.Jobs;

namespace Service.MatchingEngine.PriceSource.Services
{
    public class TradeVolumeAggregator : ITradeVolumeAggregator, IStartable, IDisposable
    {
        private readonly IPublisher<TradeVolume> _publisher;
        private readonly ILogger<TradeVolumeAggregator> _logger;

        public TradeVolumeAggregator(IPublisher<TradeVolume> publisher, ILogger<TradeVolumeAggregator> logger)
        {
            _publisher = publisher;
            _logger = logger;
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
                _logger.LogTrace("Generate trade price: {brokerId}:{symbol} {price} | {volume} | {timestampText}",
                    trade.LiquidityProvider, trade.Id, trade.Price, trade.Volume, trade.DateTime.ToString("O"));
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