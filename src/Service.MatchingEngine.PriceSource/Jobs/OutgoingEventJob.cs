using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Domain.Prices;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class OutgoingEventJob
    {
        private readonly ITradeVolumeAggregator _tradeVolumeAggregator;
        private readonly IOrderBookAggregator _orderBookAggregator;
        private long _lastSequenceId = 0;

        public OutgoingEventJob(
            ISubscriber<IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent>> subscriber,
            ILogger<OutgoingEventJob> logger,
            ITradeVolumeAggregator tradeVolumeAggregator,
            IOrderBookAggregator orderBookAggregator)
        {
            _tradeVolumeAggregator = tradeVolumeAggregator;
            _orderBookAggregator = orderBookAggregator;
            subscriber.Subscribe(HandleEvents);
        }

        private async ValueTask HandleEvents(IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent> events)
        {
            var taskList = new List<Task>();

            foreach (var outgoingEvent in events.Where(e => e.Header.SequenceNumber > _lastSequenceId && e.Orders.Any(e => e.AssetPairId=="BTCEUR")))
            {
                var updatedOrders = outgoingEvent
                    .Orders
                    .Select(e => new OrderBookOrder(
                        e.BrokerId,
                        e.AccountId,
                        e.WalletId,
                        e.ExternalId,
                        decimal.Parse(e.Price),
                        decimal.Parse(e.RemainingVolume),
                        MapSide(e.Side),
                        outgoingEvent.Header.SequenceNumber,
                        e.AssetPairId,
                        outgoingEvent.Header.Timestamp.ToDateTime(),
                        OrderIsActive(e.Status)))
                    .ToList();

                var trades = outgoingEvent
                        .Orders.SelectMany(i => i
                            .Trades
                            .Where(t => t.Role == ME.Contracts.OutgoingMessages.Order.Types.Trade.Types.TradeRole.Taker)
                            .Select(t => new
                            {
                                outgoingEvent.Header.SequenceNumber,
                                i.AssetPairId,
                                i.Side,
                                t.Price,
                                t.BaseVolume,
                                Timestamp = t.Timestamp.ToDateTime(),
                                i.BrokerId,
                                i.AccountId
                            }))

                        .Select(e => new TradeVolume()
                        {
                            Id = e.AssetPairId,
                            LiquidityProvider = e.BrokerId,
                            Price = double.Parse(e.Price),
                            Volume = double.Parse(e.BaseVolume),
                            DateTime = e.Timestamp
                        })

                        .ToList();


                var tradesTask = _tradeVolumeAggregator.RegisterTrades(trades);
                taskList.Add(tradesTask);

                var task = _orderBookAggregator.RegisterOrderUpdates(updatedOrders);
                taskList.Add(task);
            }

            if (taskList.Any())
                await Task.WhenAll(taskList);

            _lastSequenceId = events.Max(e => e.Header.SequenceNumber);
        }

        private static bool OrderIsActive(ME.Contracts.OutgoingMessages.Order.Types.OrderStatus status)
        {
            return status == ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Placed ||
                   status == ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Replaced ||
                   status == ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.PartiallyMatched;
        }

        private OrderSide MapSide(ME.Contracts.OutgoingMessages.Order.Types.OrderSide side)
        {
            switch (side)
            {
                case ME.Contracts.OutgoingMessages.Order.Types.OrderSide.Buy: return OrderSide.Buy;
                case ME.Contracts.OutgoingMessages.Order.Types.OrderSide.Sell: return OrderSide.Sell;
            }

            return OrderSide.UnknownOrderSide;
        }
    }
}