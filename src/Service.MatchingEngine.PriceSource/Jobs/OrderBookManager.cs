using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.GrpcDataWriter;
using Service.MatchingEngine.PriceSource.Jobs.Models;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    /// <summary>
    /// Manage particular order book
    /// </summary>
    public class OrderBookManager
    {
        private readonly ILogger _logger;
        private Dictionary<string, OrderBookNoSql> _data = new Dictionary<string, OrderBookNoSql>();

        private OrderBookLevelNoSql _ask;
        private OrderBookLevelNoSql _bid;
        private BidAsk _bidAsk;

        public string BrokerId { get; }

        public string Symbol { get; }

        public OrderBookManager(string brokerId, string symbol, ILogger logger)
        {
            _logger = logger;
            BrokerId = brokerId;
            Symbol = symbol;
        }

        /// <summary>
        /// Register order and notify about best price update
        /// </summary>
        public BidAsk RegisterOrderUpdate(List<OrderBookOrder> updates, Dictionary<string, OrderBookNoSql> updateList, Dictionary<string, OrderBookNoSql> deleteList)
        {
            var priceUpdated = false;

            

            foreach (var order in updates.Where(e => e.IsActive))
            {
                var orderLevel = ConvertOrder(order);

                if (!_data.TryGetValue(order.OrderId, out var entity))
                {
                    entity = OrderBookNoSql.Create(order.BrokerId, order.Symbol, orderLevel, order.Side);
                    _data[order.OrderId] = entity;
                }

                entity.Level = orderLevel;

                updateList[entity.Level.OrderId] = entity;

                if (order.Side == OrderSide.Sell && (_ask == null || _ask?.Price > order.Price))
                {
                    _ask = entity.Level;
                    priceUpdated = true;
                }

                if (order.Side == OrderSide.Buy && (_ask == null || _bid?.Price < order.Price))
                {
                    _bid = entity.Level;
                    priceUpdated = true;
                }
            }

            foreach (var order in updates.Where(e => !e.IsActive))
            {
                var orderId = order.OrderId;

                var orderLevel = ConvertOrder(order);

                if (!_data.TryGetValue(order.OrderId, out var entity))
                {
                    entity = OrderBookNoSql.Create(order.BrokerId, order.Symbol, orderLevel, order.Side);
                }

                _data.Remove(orderId);
                deleteList[orderId] = entity;
                

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
            }

            if (priceUpdated && _ask == null)
            {
                _ask = _data.Values.Where(e => e.Side == OrderSide.Sell).OrderBy(e => e.Level.Price).FirstOrDefault()?.Level;
            }

            if (priceUpdated && _bid == null)
            {
                _bid = _data.Values.Where(e => e.Side == OrderSide.Buy).OrderByDescending(e => e.Level.Price).FirstOrDefault()?.Level;
            }

            if (_ask != null && _bid != null && _ask.Price <= _bid.Price)
            {
                _logger.LogError("NEGATIVE SPREAD {symbol}; Ask: {ask}; Bid: {bid}", Symbol, _ask.Price, _bid.Price);
                priceUpdated = false;
            }

            if (priceUpdated)
            {
                var updateTs = updates.Max(e => e.Timestamp);
                
                var bidAsk = new BidAsk
                {
                    Id = Symbol,
                    LiquidityProvider = BrokerId,
                    Ask = _ask != null ? Convert.ToDouble(_ask.Price) : 0,
                    Bid = _bid != null ? Convert.ToDouble(_bid.Price) : 0,
                    DateTime = updateTs
                };

                return bidAsk;
            }

            return null;
        }

        private OrderBookLevelNoSql ConvertOrder(OrderBookOrder order)
        {
            return new OrderBookLevelNoSql(
                Convert.ToDecimal(order.Price),
                Convert.ToDecimal(order.Volume),
                order.SequenceNumber,
                order.WalletId,
                order.OrderId);
        }

        public BidAsk SetState(IEnumerable<OrderBookNoSql> entity)
        {
            _data = entity.ToDictionary(e => e.Level.OrderId) ?? throw new ArgumentNullException(nameof(entity), "Cannot init manager with null data");

            _ask = _data.Values.Where(e => e.Side == OrderSide.Sell).OrderBy(e => e.Level.Price).FirstOrDefault()?.Level;
            _bid = _data.Values.Where(e => e.Side == OrderSide.Buy).OrderByDescending(e => e.Level.Price).FirstOrDefault()?.Level;
            
            var bidAsk = new BidAsk
            {
                Id = Symbol,
                LiquidityProvider = BrokerId,
                Ask = _ask != null ? Convert.ToDouble(_ask.Price) : 0,
                Bid = _bid != null ? Convert.ToDouble(_bid.Price) : 0,
                DateTime = DateTime.UtcNow
            };

            return bidAsk;
        }
    }
}