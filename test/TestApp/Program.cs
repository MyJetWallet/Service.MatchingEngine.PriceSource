using System;
using System.Linq;
using System.Threading.Tasks;
using MyNoSqlServer.DataReader;
using ProtoBuf.Grpc.Client;
using Service.MatchingEngine.PriceSource.Client;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

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
                    foreach (var level in book.SellLevels.OrderByDescending(e => e.Price))
                    {
                        Console.WriteLine($"\t{level.Price}\t{level.Volume}");
                    }

                    Console.WriteLine();

                    foreach (var level in book.BuyLevels.OrderByDescending(e => e.Price))
                    {
                        Console.WriteLine($"{level.Volume}\t{level.Price}");
                    }
                }



                Console.WriteLine();
                Console.WriteLine();
                Console.Write("Symbol: ");
                cmd = Console.ReadLine();
            }



            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
