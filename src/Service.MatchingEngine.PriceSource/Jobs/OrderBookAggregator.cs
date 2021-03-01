﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Domain.Prices;
using MyJetWallet.MatchingEngine.Grpc.Api;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.MatchingEngine.PriceSource.Jobs.Models;
using Service.MatchingEngine.PriceSource.MyNoSql;
using Service.MatchingEngine.PriceSource.Services;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class OrderBookAggregator : IOrderBookAggregator, IDisposable
    {
        private readonly ILogger<OrderBookAggregator> _logger;
        private readonly IMyNoSqlServerDataWriter<OrderBookNoSql> _writer;
        private readonly IQuotePublisher _publisher;
        private readonly IOrderBookServiceClient _bookServiceClient;
        private readonly object _gate = new object();
        private readonly MyTaskTimer _timer;
        private bool _isInit = false;

        private readonly Dictionary<string, Dictionary<string, OrderBookManager>> _data = new Dictionary<string, Dictionary<string, OrderBookManager>>();

        private Dictionary<(string, string), string> _changedList = new Dictionary<(string, string), string>();

        public OrderBookAggregator(ILogger<OrderBookAggregator> logger, IMyNoSqlServerDataWriter<OrderBookNoSql> writer, IQuotePublisher publisher,
            IOrderBookServiceClient bookServiceClient)
        {
            _logger = logger;
            _writer = writer;
            _publisher = publisher;
            _bookServiceClient = bookServiceClient;
            _timer = new MyTaskTimer(nameof(OrderBookAggregator), TimeSpan.FromMilliseconds(1000),  logger, DoProcess);
        }

        private async Task DoProcess()
        {
            var books = new List<OrderBookNoSql>();

            var sw = new Stopwatch();
            sw.Start();

            Dictionary<(string, string), string> changes;
            lock (_gate)
            {
                changes = _changedList;
                _changedList = new Dictionary<(string, string), string>();

                foreach (var change in changes.Keys)
                {
                    var manager = GetManager(change.Item1, change.Item2);
                    books.Add(manager.GetState());
                }
            }

            try
            {
                var tasks = new List<Task>();

                var index = 0;
                while (index < books.Count)
                {
                    var task = _writer.BulkInsertOrReplaceAsync(books.Skip(10).Take(10)).AsTask();
                    index += 10;
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                //foreach (var book in books)
                //{
                //    Console.WriteLine($"Book {book.RowKey}");
                //    foreach (var level in book.SellLevels.OrderByDescending(e => e.Price))
                //    {
                //        Console.WriteLine($"\t{level.Volume}\t{level.Price}\t");
                //    }
                //    foreach (var level in book.BuyLevels.OrderByDescending(e => e.Price))
                //    {
                //        Console.WriteLine($"\t\t{level.Price}\t{level.Volume}");
                //    }
                //}
            }
            catch (Exception ex)
            {
                lock (_gate)
                {
                    foreach (var change in changes)
                    {
                        _changedList[change.Key] = change.Value;
                    }
                }

                _logger.LogError(ex, "Cannot save order books in NoSql");
            }

            sw.Stop();

            _logger.LogDebug("Time to publish order books ({count}); time: {timeText}", books.Count, sw.Elapsed.ToString());

        }
        

        public async Task RegisterOrderUpdates(List<OrderBookOrder> updates)
        {
            if (!_isInit)
                InitFromMe();

            var prices = InternalRegisterUpdates(updates);

            foreach (var bidAsk in prices)
            {
                await _publisher.Register(bidAsk);
            }
        }

        private List<BidAsk> InternalRegisterUpdates(List<OrderBookOrder> updates)
        {
            List<BidAsk> prices = new List<BidAsk>();

            lock (_gate)
            {
                foreach (var brokerUpdates in updates.GroupBy(e => e.BrokerId))
                {
                    foreach (var symbolUpdates in brokerUpdates.GroupBy(e => e.Symbol))
                    {
                        var manager = GetManager(brokerUpdates.Key, symbolUpdates.Key);
                        var priceUpdated = manager.RegisterOrderUpdate(symbolUpdates.ToList());

                        if (priceUpdated)
                        {
                            prices.Add(manager.GetBestPrices());
                        }

                        _changedList[(brokerUpdates.Key, symbolUpdates.Key)] = manager.Symbol;
                    }
                }
            }

            return prices;
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
                manager = new OrderBookManager(broker, symbol);
                brokerItem[symbol] = manager;
            }

            return manager;
        }

        public void Start()
        {
            _timer.Start();
        }

        private void InitFromMe()
        {
            _logger.LogInformation($"OrderBookAggregator stat init process");

            var books = _bookServiceClient.OrderBookSnapshots();

            var sw = new Stopwatch();
            sw.Start();
            lock (_gate)
            {
                var list = new List<OrderBookOrder>();

                foreach (var snapshot in books)
                {
                    foreach (var level in snapshot.Levels)
                    {
                        list.Add(new OrderBookOrder(snapshot.BrokerId, "", level.WalletId, level.OrderId,
                            decimal.Parse(level.Price),
                            decimal.Parse(level.Volume),
                            snapshot.IsBuy ? OrderSide.Buy : OrderSide.Sell,
                            0,
                            snapshot.Asset,
                            snapshot.Timestamp.ToDateTime(),
                            true));
                    }

                    _changedList[(snapshot.BrokerId, snapshot.Asset)] = snapshot.Asset;
                }

                InternalRegisterUpdates(list);
            }

            _isInit = true;

            sw.Stop();

            _logger.LogInformation($"OrderBookAggregator init time: {sw.Elapsed.ToString()}");
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}