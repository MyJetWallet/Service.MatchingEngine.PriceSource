using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;
using MyNoSqlServer.Abstractions;

namespace Service.MatchingEngine.PriceSource.MyNoSql
{
    public class OrderBookNoSql : MyNoSqlDbEntity
    {
        private const string Separator = "::";

        public const string TableName = "myjetwallet-spot-order-books-v2";

        public static string GeneratePartitionKey(string brokerId, string symbol) => $"{brokerId}{Separator}{symbol}";
        public static string GenerateRowKey(string orderId) => orderId;

        public static (string, string) GetBrokerIdAndSymbol(string partitionKey)
        {
            var prm = partitionKey.Split(Separator);
            return (prm[0], prm[1]);
        }

        public OrderBookLevelNoSql Level { get; set; }

        public OrderSide Side { get; set; }


        public static OrderBookNoSql Create(string brokerId, string symbol, OrderBookLevelNoSql level, OrderSide side)
        {
            return new OrderBookNoSql()
            {
                PartitionKey = GeneratePartitionKey(brokerId, symbol),
                RowKey = GenerateRowKey(level.OrderId),
                Level = level,
                Side = side
            };
        }
    }

    [DataContract]
    public class OrderBookLevelNoSql
    {
        public OrderBookLevelNoSql(decimal price, decimal volume, long sequenceId, string walletId, string orderId)
        {
            Price = price;
            Volume = volume;
            SequenceId = sequenceId;
            WalletId = walletId;
            OrderId = orderId;
        }

        public OrderBookLevelNoSql()
        {
        }

        [DataMember(Order = 1)] public decimal Price { get; set; }
        [DataMember(Order = 2)] public decimal Volume { get; set; }
        [DataMember(Order = 3)] public decimal SequenceId { get; set; }
        [DataMember(Order = 4)] public string WalletId { get; set; }
        [DataMember(Order = 5)] public string OrderId { get; set; }
    }
}