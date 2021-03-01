using System;
using MyJetWallet.Domain.Orders;

namespace Service.MatchingEngine.PriceSource.Jobs.Models
{
    public class TradeSignalVolume
    {
        public TradeSignalVolume(double price, double volume, string symbol, DateTime timestamp, OrderSide side)
        {
            Price = price;
            Volume = Math.Abs(volume);
            Side = side;
            Symbol = symbol;
            Timestamp = timestamp;
        }

        public double Price { get; set; }
        public double Volume { get; set; }
        public OrderSide Side { get; set; }
        public string Symbol { get; set; }
        public DateTime Timestamp { get; set; }
    }
}