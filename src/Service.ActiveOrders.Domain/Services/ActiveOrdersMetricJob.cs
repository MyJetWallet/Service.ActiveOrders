using System;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Prometheus;

namespace Service.ActiveOrders.Domain.Services
{
    public class ActiveOrdersMetricJob : IStartable, IDisposable
    {
        private readonly ILogger<ActiveOrdersMetricJob> _logger;
        private readonly MyTaskTimer _timer;

        private readonly HttpClient _client;

        private static readonly Gauge OrderCountsCount = Prometheus.Metrics
            .CreateGauge("spot_active_order_lp_count", "Count of active orders on account SP-LP-account spot.");

        public ActiveOrdersMetricJob(string nosqlWriterUrl, ILogger<ActiveOrdersMetricJob> logger)
        {
            _client = new HttpClient()
            {
                BaseAddress = new Uri(nosqlWriterUrl)
            };   
            _logger = logger;

            _timer = new MyTaskTimer(nameof(ActiveOrdersMetricJob), TimeSpan.FromMinutes(1), _logger, DoTime);
        }

        private async Task DoTime()
        {
            var res = await _client.GetStringAsync("/Count?tableName=myjetwallet-client-active-orders-full&partitionKey=SP-LP-account");

            var count = int.Parse(res);

            OrderCountsCount.Set(count);

            _logger.LogDebug("Count orders in 'SP-LP-account': {count}", count);
        }


        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}