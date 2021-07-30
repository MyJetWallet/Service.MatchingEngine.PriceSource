using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using ME.Contracts.OutgoingMessages;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.GrpcDataWriter;
using Newtonsoft.Json;
using Service.MatchingEngine.PriceSource.Jobs.Models;
using Service.MatchingEngine.PriceSource.MyNoSql;
using Service.MatchingEngine.PriceSource.Services;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class OutgoingEventJob
    {
        private readonly ILogger<OutgoingEventJob> _logger;
        private readonly ITradeVolumeAggregator _tradeVolumeAggregator;
        private readonly MyNoSqlGrpcDataWriter _lastSeqWriter;
        private readonly IOrderBookAggregator _orderBookAggregator;
        private long _lastSequenceId = 0;
        private int _maxBatchSize;

        public OutgoingEventJob(
            ISubscriber<IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent>> subscriber,
            ILogger<OutgoingEventJob> logger,
            ITradeVolumeAggregator tradeVolumeAggregator,
            MyNoSqlGrpcDataWriter lastSeqWriter,
            IOrderBookAggregator orderBookAggregator)
        {
            _logger = logger;
            _tradeVolumeAggregator = tradeVolumeAggregator;
            _lastSeqWriter = lastSeqWriter;
            _orderBookAggregator = orderBookAggregator;

            _maxBatchSize = Program.Settings.MaxMeEventsBatchSize;

            subscriber.Subscribe(HandleEvents);
        }

        private async ValueTask HandleEvents(IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent> events)
        {
            using var _ = MyTelemetry.StartActivity("Handle event OutgoingEvent")?.AddTag("event-count", events.Count);

            var sw = new Stopwatch();
            sw.Start();

            if (events.Count == 0)
                return;
            
            var minSecNum = events.Min(e => e.Header.SequenceNumber);
            var maxSecNum = events.Max(e => e.Header.SequenceNumber);

            if (_lastSequenceId > 0 && _lastSequenceId != minSecNum - 1)
            {
                _logger.LogError($"Miss events from {_lastSequenceId + 1} to {minSecNum - 1}  (lid: {_lastSequenceId})");
            }

            if (maxSecNum - minSecNum + 1 != events.Count)
            {
                _logger.LogError($"Receive batch with miss events. Min: {minSecNum}; max: {maxSecNum}; count: {events.Count}");
            }


            var index = 0;
            while (index < events.Count)
            {
                var eventsToHandle = events.Skip(index).Take(_maxBatchSize).ToList();

                try
                {
                    var updatedOrders = new List<OrderBookOrder>();

                    foreach (var outgoingEvent in eventsToHandle)
                    {
                        var list = outgoingEvent
                            .Orders
                            .Select(e => new OrderBookOrder(
                                e.BrokerId,
                                e.WalletId,
                                e.ExternalId,
                                string.IsNullOrEmpty(e.Price) ? 0 : decimal.Parse(e.Price),
                                string.IsNullOrEmpty(e.RemainingVolume) ? 0 : decimal.Parse(e.RemainingVolume),
                                MapSide(e.Side),
                                outgoingEvent.Header.SequenceNumber,
                                e.AssetPairId,
                                outgoingEvent.Header.Timestamp.ToDateTime(),
                                OrderIsActive(e.Status)))
                            .OrderBy(e => e.SequenceNumber)
                            .ThenByDescending(e => e.IsActive ? 1 : 0)
                            .ToList();

                        updatedOrders.AddRange(list);

                        #region trade
                        //var trades = outgoingEvent
                        //    .Orders.SelectMany(i => i
                        //        .Trades
                        //        .Where(t => t.Role == ME.Contracts.OutgoingMessages.Order.Types.Trade.Types.TradeRole.Taker)
                        //        .Select(t => new
                        //        {
                        //            outgoingEvent.Header.SequenceNumber,
                        //            i.AssetPairId,
                        //            i.Side,
                        //            t.Price,
                        //            t.BaseVolume,
                        //            Timestamp = t.Timestamp.ToDateTime(),
                        //            i.BrokerId,
                        //            i.AccountId
                        //        }))

                        //    .Select(e => new TradeVolume()
                        //    {
                        //        Id = e.AssetPairId,
                        //        LiquidityProvider = e.BrokerId,
                        //        Price = double.Parse(e.Price),
                        //        Volume = double.Parse(e.BaseVolume),
                        //        DateTime = e.Timestamp
                        //    })

                        //    .ToList();



                        //var tradesTask = _tradeVolumeAggregator.RegisterTrades(trades);
                        //taskList.Add(tradesTask);
                        #endregion
                    }

                    await _orderBookAggregator.RegisterOrderUpdates(updatedOrders);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot handle {count} Me events", eventsToHandle.Count);
                    if (_maxBatchSize > 50)
                    {
                        _maxBatchSize = _maxBatchSize / 2;
                        Console.WriteLine($"Batch size decreased to {_maxBatchSize}");
                    }

                    throw;
                }

                index += eventsToHandle.Count;
            }

            if (_maxBatchSize != Program.Settings.MaxMeEventsBatchSize)
            {
                _maxBatchSize = Program.Settings.MaxMeEventsBatchSize;
                Console.WriteLine($"Batch size restored to {_maxBatchSize}");
            }

            var prev = _lastSequenceId;
            _lastSequenceId = maxSecNum;

            sw.Stop();

            _logger.LogInformation("Handled {count} events. Time: {timeRangeText}. MinNumber: {minSecNum}, MaxNumber: {maxSecNum}", events.Count, sw.Elapsed.ToString(), minSecNum, maxSecNum);
        }

        public static bool OrderIsActive(ME.Contracts.OutgoingMessages.Order.Types.OrderStatus status)
        {
            return status == ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Placed ||
                   status == ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.PartiallyMatched;
        }

        public static OrderSide MapSide(ME.Contracts.OutgoingMessages.Order.Types.OrderSide side)
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