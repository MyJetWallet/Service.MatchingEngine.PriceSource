using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.GrpcDataWriter;
using Service.MatchingEngine.PriceSource.Jobs.Models;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class OrderBookAggregator : IOrderBookAggregator
    {
        private readonly ILogger<OrderBookAggregator> _logger;
        private readonly MyNoSqlGrpcDataWriter _writer;
        private readonly object _gate = new object();
        private bool _isInit = false;

        private readonly IPublisher<MyJetWallet.Domain.Prices.BidAsk> _pricePublisher;
        private readonly IPublisher<SimpleTrading.Abstraction.BidAsk.IBidAsk> _candlePublisher;

        private readonly Dictionary<string, Dictionary<string, OrderBookManager>> _data = new Dictionary<string, Dictionary<string, OrderBookManager>>();

        public OrderBookAggregator(
            ILogger<OrderBookAggregator> logger,
            MyNoSqlGrpcDataWriter writer, IPublisher<MyJetWallet.Domain.Prices.BidAsk> pricePublisher, IPublisher<SimpleTrading.Abstraction.BidAsk.IBidAsk> candlePublisher)
        {
            _logger = logger;
            _writer = writer;
            _pricePublisher = pricePublisher;
            _candlePublisher = candlePublisher;
        }
        

        public async Task RegisterOrderUpdates(List<OrderBookOrder> updates)
        {
            var prices = new List<MyJetWallet.Domain.Prices.BidAsk>();
            var updateList = new Dictionary<string, OrderBookNoSql>();
            var deleteList = new Dictionary<string, OrderBookNoSql>();

            lock (_gate)
            {

                if (!_isInit)
                {
                    throw new Exception($"{nameof(OrderBookAggregator)} does not inited!");
                }

                foreach (var brokerUpdates in updates.GroupBy(e => e.BrokerId))
                {
                    foreach (var symbolUpdates in brokerUpdates.GroupBy(e => e.Symbol))
                    {
                        var manager = GetManager(brokerUpdates.Key, symbolUpdates.Key);
                        var price = manager.RegisterOrderUpdate(symbolUpdates.ToList(), updateList, deleteList);

                        if (price != null)
                        {
                            if (price.Ask > 0 && price.Ask <= price.Bid)
                            {
                                _logger.LogError("Detect negative spread in order book {symbol}. Ask: {ask}; Bid: {bid}", price.Id, price.Ask, price.Bid);
                            }
                            else
                            {
                                prices.Add(price);
                            }
                        }
                    }
                }
            }

            var transaction = _writer.BeginTransaction();

            var entities = updateList.Values.Where(e => !deleteList.ContainsKey(e.Level.OrderId));

            transaction.InsertOrReplaceEntities(entities);

            foreach (var group in deleteList.Values.GroupBy(e => e.PartitionKey))
            {
                transaction.DeleteRows(OrderBookNoSql.TableName, group.Key, group.Select(e => e.RowKey).ToArray());
            }

            var taskList = new List<Task>();


            var priceEntities = prices.Select(BidAskNoSql.Create).ToList();
            transaction.InsertOrReplaceEntities(priceEntities);

            taskList.Add(transaction.CommitAsync().AsTask());

            foreach (var bidAsk in prices)
            {
                taskList.Add(PublishSpotQuote(bidAsk));
                taskList.Add(PublishCandlePrice(bidAsk));
            }

            await Task.WhenAll(taskList);
        }

        private async Task PublishSpotQuote(MyJetWallet.Domain.Prices.BidAsk quote)
        {
            try
            {
                await _pricePublisher.PublishAsync(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot publish price to spot service bus {symbol}", quote.Id);
                throw;
            }
        }

        private async Task PublishCandlePrice(MyJetWallet.Domain.Prices.BidAsk quote)
        {
            try
            {
                var message = new SimpleTrading.ServiceBus.Models.BidAskServiceBusModel()
                {
                    Id = quote.Id,
                    Ask = quote.Ask,
                    Bid = quote.Bid,
                    DateTime = quote.DateTime
                };

                await _candlePublisher.PublishAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot publish price to Candle service bus {symbol}", quote.Id);
                throw;
            }
        }
        
        private OrderBookManager GetManager(string broker, string symbol)
        {
            if (!_data.TryGetValue(broker, out var brokerItem))
            {
                brokerItem = new Dictionary<string, OrderBookManager>();
                _data[broker] = brokerItem;
            }

            if (!brokerItem.TryGetValue(symbol, out var manager))
            {
                manager = new OrderBookManager(broker, symbol, _logger);
                brokerItem[symbol] = manager;
            }

            return manager;
        }

        public void Start()
        {
            var data = _writer.GetRowsAsync<OrderBookNoSql>().ToListAsync().GetAwaiter().GetResult();

            lock (_gate)
            {
                foreach (var book in data.GroupBy(e => e.PartitionKey))
                {
                    var (brokerId, symbol) = OrderBookNoSql.GetBrokerIdAndSymbol(book.Key);
                    var manager = new OrderBookManager(brokerId, symbol, _logger);
                    var price = manager.SetState(book);

                    if (!_data.TryGetValue(brokerId, out var symbolManagers))
                    {
                        symbolManagers = new Dictionary<string, OrderBookManager>();
                        _data[brokerId] = symbolManagers;
                    }

                    symbolManagers[symbol] = manager;

                    _logger.LogInformation("Book manager is inited. BrokerId: {brokerId}; Symbol:{symbol}; Count: {count}. Price: {ask} / {bid}", brokerId, symbol, book.Count(), price.Ask, price.Bid);
                }

                _isInit = true;
            }
        }
    }
}