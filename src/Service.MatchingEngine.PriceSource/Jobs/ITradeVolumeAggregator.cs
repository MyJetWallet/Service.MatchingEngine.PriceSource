using System;
using System.Collections.Generic;
using Autofac;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public interface ITradeVolumeAggregator
    {
        void RegisterTrades(List<TradeSignalVolume> trades);
    }

    public class TradeVolumeAggregator : ITradeVolumeAggregator, IStartable, IDisposable
    {
        public void RegisterTrades(List<TradeSignalVolume> trades)
        {
            
        }

        public void Start()
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}