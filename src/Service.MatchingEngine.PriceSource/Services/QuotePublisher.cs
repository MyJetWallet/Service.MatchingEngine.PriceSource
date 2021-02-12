using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Prices;
using MyNoSqlServer.Abstractions;
using Service.MatchingEngine.PriceSource.MyNoSql;

namespace Service.MatchingEngine.PriceSource.Services
{
    public class QuotePublisher : IQuotePublisher, IStartable, IDisposable
    {
        public const int WriterDelayMs = 1000;

        private readonly IPublisher<BidAsk> _publisher;
        private readonly IMyNoSqlServerDataWriter<BidAskNoSql> _writer;
        private readonly ILogger<QuotePublisher> _logger;
        private CancellationTokenSource _token = new CancellationTokenSource();
        private Task _processWriter;
        private readonly object _gate = new object();

        private List<BidAsk> _buffer = new List<BidAsk>();


        public QuotePublisher(IPublisher<BidAsk> publisher, IMyNoSqlServerDataWriter<BidAskNoSql> writer, ILogger<QuotePublisher> logger)
        {
            _publisher = publisher;
            _writer = writer;
            _logger = logger;
        }

        private async Task DoProcess()
        {
            while (Equals(!_token.IsCancellationRequested))
            {
                try
                {
                    List<BidAsk> buffer;

                    if (!_buffer.Any())
                        return;

                    lock (_gate)
                    {
                        buffer = _buffer;
                        _buffer = new List<BidAsk>(128);
                    }

                    foreach (var group in buffer.GroupBy(e => e.Id))
                    {
                        var last = group.OrderByDescending(e => e.DateTime).First();
                        await _writer.InsertOrReplaceAsync(BidAskNoSql.Create(last));
                    }

                    await Task.Delay(WriterDelayMs);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "QuotePublisher unexpected exception");
                }
            }
        }

        public async Task Register(BidAsk quote)
        {
            lock (_gate) _buffer.Add(quote);
            await _publisher.PublishAsync(quote);
        }

        public void Start()
        {
            _processWriter = Task.Run(DoProcess, _token.Token);
        }

        public void Dispose()
        {
            _token.Cancel();
            _processWriter.Wait();
        }
    }
}