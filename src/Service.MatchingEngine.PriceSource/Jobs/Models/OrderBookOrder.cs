using System;
using MyJetWallet.Domain.Orders;

namespace Service.MatchingEngine.PriceSource.Jobs.Models
{
    public class OrderBookOrder
    {
        public OrderBookOrder(string brokerId, string walletId, string orderId, decimal price, decimal volume, OrderSide side, long sequenceNumber, string symbol, DateTime timestamp, bool isActive)
        {
            BrokerId = brokerId;
            WalletId = walletId;
            OrderId = orderId;
            Price = price;
            Volume = volume;
            Side = side;
            SequenceNumber = sequenceNumber;
            Symbol = symbol;
            Timestamp = timestamp;
            IsActive = isActive;
        }

        public OrderBookOrder(string symbol, DateTime timestamp, bool isActive)
        {
            Symbol = symbol;
            Timestamp = timestamp;
            IsActive = isActive;
        }

        public string BrokerId { get; set; }
        public string WalletId { get; set; }
        public string OrderId { get; set; }

        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public OrderSide Side { get; set; }
        public string Symbol { get; set; }

        public long SequenceNumber { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsActive { get; set; }
    }
}