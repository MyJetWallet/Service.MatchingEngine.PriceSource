using System;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.Abstractions;

namespace Service.MatchingEngine.PriceSource.MyNoSql
{
    public class BidAskNoSql: MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-bitask-last";

        public static string GeneratePartitionKey(string brokerId) => brokerId;
        public static string GenerateRowKey(string symbol) => symbol;

        public BidAsk Quote { get; set; }

        public static BidAskNoSql Create(BidAsk quote)
        {
            return new BidAskNoSql()
            {
                PartitionKey = BidAskNoSql.GeneratePartitionKey(quote.LiquidityProvider),
                RowKey = BidAskNoSql.GenerateRowKey(quote.Id),
                Quote = quote
            };
        }
    }
}
