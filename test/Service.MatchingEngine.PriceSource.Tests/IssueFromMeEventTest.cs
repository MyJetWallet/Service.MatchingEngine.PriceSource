using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Service.MatchingEngine.PriceSource.Jobs;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Tests
{
    public class IssueFromMeEventTest
    {
        [Test]
        public void Test1()
        {
            var json =
                "{\"Header\":{\"MessageType\":4,\"SequenceNumber\":478300,\"MessageId\":\"f0274b527bc34fe48608d67384794434\",\"RequestId\":\"14905114d81546a8b0c90954a2ffbedc\",\"Version\":\"1\",\"Timestamp\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"EventType\":\"MARKET_ORDER\"},\"BalanceUpdates\":[{\"BrokerId\":\"jetwallet\",\"AccountId\":\"alex\",\"WalletId\":\"SP-alex\",\"WalletVersion\":30,\"AssetId\":\"BTC\",\"OldBalance\":\"0.01243\",\"NewBalance\":\"0.01242801\",\"OldReserved\":\"0\",\"NewReserved\":\"0\"},{\"BrokerId\":\"jetwallet\",\"AccountId\":\"alex\",\"WalletId\":\"SP-alex\",\"WalletVersion\":3,\"AssetId\":\"EUR\",\"OldBalance\":\"1000000\",\"NewBalance\":\"1000000.1\",\"OldReserved\":\"0\",\"NewReserved\":\"0\"},{\"BrokerId\":\"jetwallet\",\"AccountId\":\"LPFTX\",\"WalletId\":\"SP-LPFTX\",\"WalletVersion\":1652007,\"AssetId\":\"EUR\",\"OldBalance\":\"101000159.97\",\"NewBalance\":\"101000159.87\",\"OldReserved\":\"501547.02\",\"NewReserved\":\"501546.92\"},{\"BrokerId\":\"jetwallet\",\"AccountId\":\"LPFTX\",\"WalletId\":\"SP-LPFTX\",\"WalletVersion\":1377914,\"AssetId\":\"BTC\",\"OldBalance\":\"10010.79799999\",\"NewBalance\":\"10010.79800198\",\"OldReserved\":\"10\",\"NewReserved\":\"10\"}],\"CashIn\":null,\"CashOut\":null,\"CashTransfer\":null,\"Orders\":[{\"BrokerId\":\"jetwallet\",\"AccountId\":\"LPFTX\",\"WalletId\":\"SP-LPFTX\",\"Id\":\"aeb25951-3f25-45c7-8026-6960b755cc65\",\"ExternalId\":\"635f792a6fb9471aa3e8e49ecbf2590c-7\",\"AssetPairId\":\"BTCEUR\",\"OrderType\":2,\"Side\":1,\"Volume\":\"0.0595\",\"RemainingVolume\":\"0.05949801\",\"Price\":\"50349.6\",\"Status\":2,\"RejectReason\":\"\",\"StatusDate\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"CreatedAt\":{\"Seconds\":1617371912,\"Nanos\":582000000},\"Registered\":{\"Seconds\":1617371912,\"Nanos\":585000000},\"LastMatchTime\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"LowerLimitPrice\":\"\",\"LowerPrice\":\"\",\"UpperLimitPrice\":\"\",\"UpperPrice\":\"\",\"Straight\":false,\"Fees\":[],\"Trades\":[{\"TradeId\":\"a53b38b2-df1d-4e6a-a4a3-9463c5384e14\",\"BaseAssetId\":\"BTC\",\"BaseVolume\":\"0.00000199\",\"Price\":\"50349.6\",\"Timestamp\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"OppositeOrderId\":\"575f6ce9-5bcc-48f8-89f0-88781ddbc7b8\",\"OppositeExternalOrderId\":\"14905114d81546a8b0c90954a2ffbedc\",\"OppositeWalletId\":\"SP-alex\",\"QuotingAssetId\":\"EUR\",\"QuotingVolume\":\"-0.1\",\"Index\":0,\"AbsoluteSpread\":\"162.87\",\"RelativeSpread\":\"0.0032\",\"Role\":1,\"Fees\":[]}],\"TimeInForce\":1,\"ExpiryTime\":null,\"ParentExternalId\":\"\",\"ChildExternalId\":\"\"},{\"BrokerId\":\"jetwallet\",\"AccountId\":\"alex\",\"WalletId\":\"SP-alex\",\"Id\":\"575f6ce9-5bcc-48f8-89f0-88781ddbc7b8\",\"ExternalId\":\"14905114d81546a8b0c90954a2ffbedc\",\"AssetPairId\":\"BTCEUR\",\"OrderType\":1,\"Side\":1,\"Volume\":\"0.1\",\"RemainingVolume\":\"\",\"Price\":\"50349.6\",\"Status\":3,\"RejectReason\":\"\",\"StatusDate\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"CreatedAt\":{\"Seconds\":0,\"Nanos\":0},\"Registered\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"LastMatchTime\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"LowerLimitPrice\":\"\",\"LowerPrice\":\"\",\"UpperLimitPrice\":\"\",\"UpperPrice\":\"\",\"Straight\":false,\"Fees\":[],\"Trades\":[{\"TradeId\":\"a53b38b2-df1d-4e6a-a4a3-9463c5384e14\",\"BaseAssetId\":\"BTC\",\"BaseVolume\":\"-0.00000199\",\"Price\":\"50349.6\",\"Timestamp\":{\"Seconds\":1617371913,\"Nanos\":93000000},\"OppositeOrderId\":\"aeb25951-3f25-45c7-8026-6960b755cc65\",\"OppositeExternalOrderId\":\"635f792a6fb9471aa3e8e49ecbf2590c-7\",\"OppositeWalletId\":\"SP-LPFTX\",\"QuotingAssetId\":\"EUR\",\"QuotingVolume\":\"0.1\",\"Index\":0,\"AbsoluteSpread\":\"162.87\",\"RelativeSpread\":\"0.0032\",\"Role\":2,\"Fees\":[]}],\"TimeInForce\":0,\"ExpiryTime\":null,\"ParentExternalId\":\"\",\"ChildExternalId\":\"\"}]}";

            var outgoingEvent = JsonConvert.DeserializeObject<ME.Contracts.OutgoingMessages.OutgoingEvent>(json);

            foreach (var order in outgoingEvent.Orders)
            {
                var e = order;

                var price = decimal.Parse(e.Price);
                var volume = string.IsNullOrEmpty(e.RemainingVolume) ? 0 : decimal.Parse(e.RemainingVolume);


                var item = new OrderBookOrder(
                    e.BrokerId,
                    e.WalletId,
                    e.ExternalId,
                    price,
                    volume,
                    OutgoingEventJob.MapSide(e.Side),
                    outgoingEvent.Header.SequenceNumber,
                    e.AssetPairId,
                    outgoingEvent.Header.Timestamp.ToDateTime(),
                    OutgoingEventJob.OrderIsActive(e.Status));

                Assert.NotNull(item);
            }

            var updatedOrders = outgoingEvent
                .Orders
                .Select(e => new OrderBookOrder(
                    e.BrokerId,
                    e.WalletId,
                    e.ExternalId,
                    decimal.Parse(e.Price),
                    string.IsNullOrEmpty(e.RemainingVolume) ? 0 : decimal.Parse(e.RemainingVolume),
                    OutgoingEventJob.MapSide(e.Side),
                    outgoingEvent.Header.SequenceNumber,
                    e.AssetPairId,
                    outgoingEvent.Header.Timestamp.ToDateTime(),
                    OutgoingEventJob.OrderIsActive(e.Status)))
                .ToList();

            Assert.NotNull(updatedOrders);
        }
    }
}