using System;
using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Domain.Orders;
using NUnit.Framework;
using Service.MatchingEngine.PriceSource.Jobs;
using Service.MatchingEngine.PriceSource.Jobs.Models;

namespace Service.MatchingEngine.PriceSource.Tests
{
    public class OrderBookManagerTest
    {
        private string broker;
        private string symbol;
        private string client1;
        private string wallet1;
        private string client2;
        private string wallet2;
        private OrderBookManager manager;

        [SetUp]
        public void Setup()
        {
            broker = "jetwallet";
            symbol = "BTCEUR";
            client1 = "client1";
            wallet1 = "wallet1";
            client2 = "client2";
            wallet2 = "wallet3";

            manager = new OrderBookManager(broker, symbol);
        }

        [Test]
        public void Test1()
        {
            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "1", 80014, -0.001m, OrderSide.Sell, 1, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            var price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(0m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "2", 80000, -0.001m, OrderSide.Sell, 2, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80000m, price.Ask);
            Assert.AreEqual(0m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "2", 80000, -0.001m, OrderSide.Sell, 3, symbol,
                    DateTime.UtcNow, false),
                new OrderBookOrder(broker, wallet2, "3", 80000, 0.001m, OrderSide.Buy, 3, symbol,
                    DateTime.UtcNow, false),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(0m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "4", 79980, 0.001m, OrderSide.Buy, 4, symbol,
                    DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            var state = manager.GetState();

            Assert.AreEqual(1, state.BuyLevels.Count);
            Assert.AreEqual(79980, state.BuyLevels.First().Price);
            Assert.AreEqual(0.001m, state.BuyLevels.First().Volume);

            Assert.AreEqual(1, state.SellLevels.Count);
            Assert.AreEqual(80014, state.SellLevels.First().Price);
            Assert.AreEqual(-0.001m, state.SellLevels.First().Volume);
        }

        [Test]
        public void Test2()
        {
            Test1();

            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "5", 80020, -0.001m, OrderSide.Sell, 5, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, false);
            var price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 6, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 7, symbol,
                    DateTime.UtcNow, false),
                new OrderBookOrder(broker, wallet2, "7", 80001, 0.001m, OrderSide.Buy, 7, symbol,
                    DateTime.UtcNow, false),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "8", 79970, 0.001m, OrderSide.Buy, 8, symbol,
                    DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, false);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);
        }

        [Test]
        public void Test3()
        {
            Test1();

            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "5", 80020, -0.001m, OrderSide.Sell, 5, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, false);
            var price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 6, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 7, symbol,
                    DateTime.UtcNow, false),
                new OrderBookOrder(broker, wallet2, "7", 80001, 0.001m, OrderSide.Buy, 7, symbol,
                    DateTime.UtcNow, false),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "8", 79985, 0.001m, OrderSide.Buy, 8, symbol,
                    DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79985m, price.Bid);
        }

        [Test]
        public void Test4()
        {
            Test1();

            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "5", 80020, -0.001m, OrderSide.Sell, 5, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, false);
            var price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.003m, OrderSide.Sell, 6, symbol,
                    DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.002m, OrderSide.Sell, 7, symbol,
                    DateTime.UtcNow, true),
                new OrderBookOrder(broker, wallet2, "7", 80001, 0.001m, OrderSide.Buy, 7, symbol,
                    DateTime.UtcNow, false),
            });

            Assert.AreEqual(priceUpdated, false);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "8", 79985, 0.001m, OrderSide.Buy, 8, symbol,
                    DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79985m, price.Bid);
        }


        [Test]
        public void Test5()
        {
            Test1();

            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "5", 80020, -0.001m, OrderSide.Sell, 5, symbol, DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, false);
            var price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 6, symbol, DateTime.UtcNow, true)
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80001m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "6", 80001, -0.001m, OrderSide.Sell, 7, symbol, DateTime.UtcNow, false),
                new OrderBookOrder(broker, wallet2, "7", 80001, 0.002m, OrderSide.Buy, 7, symbol, DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(80001m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "8", 79985, 0.001m, OrderSide.Buy, 8, symbol, DateTime.UtcNow, true),
            });

            Assert.AreEqual(priceUpdated, false);
            price = manager.GetBestPrices();
            Assert.AreEqual(80014m, price.Ask);
            Assert.AreEqual(80001m, price.Bid);
        }

        [Test]
        public void Test6()
        {
            Test1();

            var priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet1, "1", 80020, -0.001m, OrderSide.Sell, 5, symbol, DateTime.UtcNow, false)
            });

            Assert.AreEqual(priceUpdated, true);
            var price = manager.GetBestPrices();
            Assert.AreEqual(0m, price.Ask);
            Assert.AreEqual(79980m, price.Bid);

            priceUpdated = manager.RegisterOrderUpdate(new List<OrderBookOrder>()
            {
                new OrderBookOrder(broker, wallet2, "4", 79980, 0.001m, OrderSide.Buy, 6, symbol, DateTime.UtcNow, false),
            });

            Assert.AreEqual(priceUpdated, true);
            price = manager.GetBestPrices();
            Assert.AreEqual(0m, price.Ask);
            Assert.AreEqual(0m, price.Bid);
        }
    }
}
