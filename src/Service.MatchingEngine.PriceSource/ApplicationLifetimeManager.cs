using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyServiceBus.TcpClient;
using Service.MatchingEngine.PriceSource.Jobs;

namespace Service.MatchingEngine.PriceSource
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyServiceBusTcpClient _serviceBusTcpClient;
        private readonly OrderBookAggregator _bookAggregator;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, 
            ILogger<ApplicationLifetimeManager> logger,
            MyServiceBusTcpClient serviceBusTcpClient,
            OrderBookAggregator bookAggregator)
            : base(appLifetime)
        {
            _logger = logger;
            _serviceBusTcpClient = serviceBusTcpClient;
            _bookAggregator = bookAggregator;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _bookAggregator.Start();
            _serviceBusTcpClient.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _serviceBusTcpClient.Stop();
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
