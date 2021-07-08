using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyServiceBus.TcpClient;
using Service.ActiveOrders.Jobs;

namespace Service.ActiveOrders
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyServiceBusTcpClient _client;
        private readonly CleanupDatabaseJob _cleanupDatabaseJob;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger, MyServiceBusTcpClient client, CleanupDatabaseJob cleanupDatabaseJob)
            : base(appLifetime)
        {
            _logger = logger;
            _client = client;
            _cleanupDatabaseJob = cleanupDatabaseJob;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _client.Start();
            _logger.LogInformation("MyServiceBusTcpClient is started.");
            _cleanupDatabaseJob.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _client.Stop();
            _logger.LogInformation("MyServiceBusTcpClient is stopped.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
