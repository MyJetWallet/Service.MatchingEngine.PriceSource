using System.Collections.Generic;
using System.Runtime.Serialization;
using MyNoSqlServer.Abstractions;

namespace Service.MatchingEngine.PriceSource.MyNoSql
{
    public class OrderBookNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-spot-order-books";

        public static string GeneratePartitionKey(string brokerId) => brokerId;
        public static string GenerateRowKey(string symbol) => symbol;

        public List<OrderBookLevel> BuyLevels { get; set; }
        public List<OrderBookLevel> SellLevels { get; set; }
        public OrderBookLevel Ask { get; set; }
        public OrderBookLevel Bid { get; set; }
        public long LastSequenceId { get; set; }

        public static OrderBookNoSql Create(string brokerId, string symbol)
        {
            return new OrderBookNoSql()
            {
                PartitionKey = GeneratePartitionKey(brokerId),
                RowKey = GenerateRowKey(symbol),
                BuyLevels = new List<OrderBookLevel>(),
                SellLevels = new List<OrderBookLevel>(),
                Ask = null,
                Bid = null,
                LastSequenceId = 0
            };
        }
    }

    [DataContract]
    public class OrderBookLevel
    {
        public OrderBookLevel(decimal price, decimal volume, long sequenceId)
        {
            Price = price;
            Volume = volume;
        }

        public OrderBookLevel()
        {
        }

        [DataMember(Order = 1)] public decimal Price { get; set; }
        [DataMember(Order = 2)] public decimal Volume { get; set; }
        [DataMember(Order = 3)] public decimal SequenceId { get; set; }
    }
}