using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.DataReader;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using ProtoBuf.Grpc.Client;
using Service.MatchingEngine.PriceSource.Client;
using Service.MatchingEngine.PriceSource.MyNoSql;
using SimpleTrading.Abstraction.BidAsk;
using SimpleTrading.ServiceBus.PublisherSubscriber.BidAsk;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            //CheckNoSql();

            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            var serviceBusClient = new MyServiceBusTcpClient(() => "servicebus-test.infrastructure.svc.cluster.local:6421", "TestApp");




            Console.WriteLine("End");
            Console.ReadLine();
        }

        private static ValueTask HandleTick(IBidAsk arg)
        {
            Console.WriteLine($"{arg.Id}  {arg.Bid} | {arg.Ask}   {arg.DateTime:O}");
            return new ValueTask();
        }

        private static void CheckNoSql()
        {
            var myNoSqlClient = new MyNoSqlTcpClient(() => "192.168.10.80:5125", "TestApp");
            var subs = new MyNoSqlReadRepository<OrderBookNoSql>(myNoSqlClient, OrderBookNoSql.TableName);

            myNoSqlClient.Start();

            IOrderBookService client = new OrderBookCache(subs);

            Console.Write("Symbol: ");
            var cmd = Console.ReadLine();
            var symbol = "";

            while (cmd != "exit")
            {
                Console.Clear();
                if (!string.IsNullOrEmpty(cmd))
                    symbol = cmd;

                Console.WriteLine($"Symbol: {symbol}");
                var book = client.GetOrderBook("jetwallet", symbol);

                if (book != null)
                {
                    foreach (var level in book.Where(e => e.Volume < 0).OrderByDescending(e => e.Price))
                    {
                        Console.WriteLine($"\t{level.Price}\t{level.Volume}");
                    }

                    Console.WriteLine();

                    foreach (var level in book.Where(e => e.Volume > 0).OrderByDescending(e => e.Price))
                    {
                        Console.WriteLine($"{level.Volume}\t{level.Price}");
                    }
                }


                Console.WriteLine();
                Console.WriteLine();
                Console.Write("Symbol: ");
                cmd = Console.ReadLine();
            }
        }
    }
}
