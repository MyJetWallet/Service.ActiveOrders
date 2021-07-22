using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.GrpcDataWriter;
using Prometheus;
using Service.ActiveOrders.Domain.Models;

namespace Service.ActiveOrders.Domain.Services
{
    public class ActiveOrdersMetricJob : IStartable, IDisposable
    {
        private readonly ILogger<ActiveOrdersMetricJob> _logger;
        private readonly MyNoSqlGrpcDataWriter _writer;
        private readonly MyTaskTimer _timer;
        private readonly MyTaskTimer _timerOldOrders;

        private readonly HttpClient _client;

        private static readonly Gauge OrderCountsCount = Prometheus.Metrics.CreateGauge("spot_active_order_lp_count", "Count of active orders on account SP-LP-account spot.");
        private static readonly Gauge OldOrdersCount = Prometheus.Metrics.CreateGauge("spot_active_order_old_lp_count", "Count of active orders on account SP-LP-account spot where life time more 5 min.");

        public ActiveOrdersMetricJob(string nosqlWriterUrl, ILogger<ActiveOrdersMetricJob> logger, MyNoSqlGrpcDataWriter writer)
        {
            _client = new HttpClient()
            {
                BaseAddress = new Uri(nosqlWriterUrl)
            };   
            _logger = logger;
            _writer = writer;

            _timer = new MyTaskTimer(nameof(ActiveOrdersMetricJob), TimeSpan.FromMinutes(1), _logger, DoTime);
            _timerOldOrders = new MyTaskTimer(nameof(ActiveOrdersMetricJob), TimeSpan.FromMinutes(1), _logger, DoTimeOldOrders);
        }

        private async Task DoTimeOldOrders()
        {
            var data = await _writer.GetRowsAsync<OrderNoSqlEntity>(OrderNoSqlEntity.GeneratePartitionKey("SP-LP-account")).ToListAsync();

            var time = DateTime.UtcNow.AddMinutes(-5);

            var countOldOrders = data.Count(e => e.Order.CreatedTime <= time);

            OldOrdersCount.Set(countOldOrders);

            if (countOldOrders > 0)
            {
                _logger.LogWarning("Count OLD orders in 'SP-LP-account': {count}", OldOrdersCount);
            }
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
            _timerOldOrders.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timerOldOrders.Stop();
        }
    }
}