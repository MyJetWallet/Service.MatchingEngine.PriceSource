using System;
using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Domain.Prices;
using Service.MatchingEngine.PriceSource.Jobs.Models;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class OrderBookManager
    {
        private readonly Dictionary<string, OrderBookOrder> _orders = new Dictionary<string, OrderBookOrder>();

        private OrderBookOrder _ask;
        private OrderBookOrder _bid;
        private BidAsk _bidAsk;
        private long _lastSequenceId= -1;

        public string BrokerId { get; }

        public string Symbol { get; }

        public OrderBookManager(string brokerId, string symbol)
        {
            BrokerId = brokerId;
            Symbol = symbol;
        }

        public bool RegisterOrderUpdate(List<OrderBookOrder> updates)
        {
            var sequenceId = _lastSequenceId;
            var priceUpdated = false;

            foreach (var order in updates.Where(e => e.SequenceNumber > _lastSequenceId).Where(e => e.IsActive))
            {
                _orders[order.OrderId] = order;

                if (order.Side == OrderSide.Sell && _ask?.Price > order.Price)
                {
                    _ask = null;
                    priceUpdated = true;
                }

                if (order.Side == OrderSide.Buy && _bid?.Price < order.Price)
                {
                    _bid = null;
                    priceUpdated = true;
                }

                if (order.SequenceNumber > sequenceId)
                    sequenceId = order.SequenceNumber;
            }

            foreach (var order in updates.Where(e => e.SequenceNumber > _lastSequenceId).Where(e => !e.IsActive))
            {
                var orderId = order.OrderId;

                _orders.Remove(orderId);

                if (_ask?.OrderId == orderId)
                {
                    _ask = null;
                    priceUpdated = true;
                }

                if (_bid?.OrderId == orderId)
                {
                    _bid = null;
                    priceUpdated = true;
                }

                if (order.SequenceNumber > sequenceId)
                    sequenceId = order.SequenceNumber;
            }

            _lastSequenceId = sequenceId;
            

            if (_ask == null)
            {
                _ask = _orders.Values.Where(e => e.Side == OrderSide.Sell).OrderBy(e => e.Price).FirstOrDefault();
                priceUpdated = priceUpdated || _ask != null;
            }

            if (_bid == null)
            {
                _bid = _orders.Values.Where(e => e.Side == OrderSide.Buy).OrderByDescending(e => e.Price).FirstOrDefault();
                priceUpdated = priceUpdated || _bid != null;
            }

            if (priceUpdated)
            {
                var updateTs = updates.Any() ? updates.Max(e => e.Timestamp) : DateTime.MinValue;
                
                _bidAsk = new BidAsk
                {
                    Id = Symbol,
                    LiquidityProvider = BrokerId,
                    Ask = _ask != null ? (double) _ask.Price : 0,
                    Bid = _bid != null ? (double) _bid.Price : 0,
                    DateTime = updateTs
                };

                return true;
            }

            return false;
        }

        public OrderBookNoSql GetState()
        {
            var orderBook = OrderBookNoSql.Create(BrokerId, Symbol);

            orderBook.BuyLevels =
                _orders.Values
                    .Where(e => e.Side == OrderSide.Buy)
                    .GroupBy(e => e.Price)
                    .Select(e => new PriceSource.MyNoSql.OrderBookLevel(
                        e.Key,
                        e.Sum(i => i.Volume),
                        e.Max(i => i.SequenceNumber)))
                    .OrderByDescending(e => e.Price)
                    .ToList();

            orderBook.SellLevels =
                _orders.Values
                    .Where(e => e.Side == OrderSide.Sell)
                    .GroupBy(e => e.Price)
                    .Select(e => new PriceSource.MyNoSql.OrderBookLevel(
                        e.Key,
                        e.Sum(i => i.Volume),
                        e.Max(i => i.SequenceNumber)))
                    .OrderBy(e => e.Price)
                    .ToList();

            orderBook.Bid = orderBook.BuyLevels.FirstOrDefault();
            orderBook.Ask = orderBook.SellLevels.FirstOrDefault();

            if (_orders.Any())
            {
                orderBook.LastSequenceId = _orders.Max(e => e.Value.SequenceNumber);
            }
            
            return orderBook;
        }

        public DetailOrderBookNoSql GetDetailState()
        {
            var orderBook = DetailOrderBookNoSql.Create(BrokerId, Symbol);

            orderBook.BuyLevels =
                _orders.Values
                    .Where(e => e.Side == OrderSide.Buy)
                    .Select(e => new PriceSource.MyNoSql.OrderBookLevel(
                        e.Price,
                        e.Volume,
                        e.SequenceNumber,
                        e.WalletId,
                        e.OrderId))
                    .OrderByDescending(e => e.Price)
                    .ToList();

            orderBook.SellLevels =
                _orders.Values
                    .Where(e => e.Side == OrderSide.Sell)
                    .Select(e => new PriceSource.MyNoSql.OrderBookLevel(
                        e.Price,
                        e.Volume,
                        e.SequenceNumber,
                        e.WalletId,
                        e.OrderId))
                    .OrderBy(e => e.Price)
                    .ToList();

            orderBook.Bid = orderBook.BuyLevels.FirstOrDefault();
            orderBook.Ask = orderBook.SellLevels.FirstOrDefault();

            if (_orders.Any())
            {
                orderBook.LastSequenceId = _orders.Max(e => e.Value.SequenceNumber);
            }

            return orderBook;
        }

        public BidAsk GetBestPrices()
        {
            return _bidAsk;
        }
    }
}