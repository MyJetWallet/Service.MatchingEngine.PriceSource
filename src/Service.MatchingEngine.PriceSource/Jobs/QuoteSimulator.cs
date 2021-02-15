using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Hosting;
using MyJetWallet.Domain;
using MyJetWallet.Domain.Prices;
using Service.MatchingEngine.PriceSource.Services;

namespace Service.MatchingEngine.PriceSource.Jobs
{
    public class QuoteSimulator: IHostedService
    {
        private readonly IQuotePublisher _publisher;
        private CancellationTokenSource _token = new CancellationTokenSource();
        private Task _process;

        private readonly Dictionary<string, (int, int, int)> _settings = new Dictionary<string, (int, int, int)>();
        private readonly Dictionary<string, int> _last = new Dictionary<string, int>();

        public QuoteSimulator(IQuotePublisher publisher)
        {
            _publisher = publisher;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _settings.Add("BTCUSD", (10000, 30000, 1));
            _settings.Add("ETHUSD", (200, 600, 1));
            _settings.Add("BTCEUR", (10000, 30000, 1));
            _settings.Add("ETHEUR", (200, 600, 1));

            _last.Add("BTCUSD", 15000);
            _last.Add("ETHUSD", 15000);
            _last.Add("BTCEUR", 15000);
            _last.Add("ETHEUR", 15000);


            _process = Task.Run(DoProcess, _token.Token);
            return Task.CompletedTask;
        }

        private readonly Random _rnd = new Random();

        private async Task DoProcess()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    foreach (var setting in _settings)
                    {
                        int current;

                        lock (_last) current = _last[setting.Key];

                        {
                            var rnd = _rnd.Next(setting.Value.Item3 * 2 + 1);
                            var delta = rnd - setting.Value.Item3;
                            current += delta * _rnd.Next(10);
                            if (current < setting.Value.Item1) current = setting.Value.Item1;
                            if (current > setting.Value.Item2) current = setting.Value.Item2;
                        }

                        lock (_last) _last[setting.Key] = current;
                    }

                    foreach (var last in _last)
                    {
                        var quote = new BidAsk()
                        {
                            Id = last.Key,
                            DateTime = DateTime.UtcNow,
                            LiquidityProvider = DomainConstants.DefaultBroker,
                            Ask = last.Value + _settings[last.Key].Item3,
                            Bid = last.Value + _settings[last.Key].Item3,
                        };
                        await _publisher.Register(quote);
                    }

                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _token.Cancel();
            return _process;
        }
    }
}