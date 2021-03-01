using System;

namespace Service.MatchingEngine.PriceSource.Jobs.Models
{
    public class OrderBookDeletedOrder
    {
        public OrderBookDeletedOrder(string orderId, long sequenceNumber, DateTime timestamp)
        {
            OrderId = orderId;
            SequenceNumber = sequenceNumber;
            Timestamp = timestamp;
        }

        public string OrderId { get; set; }

        public long SequenceNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}