using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
using Service.ActiveOrders.Postgres;

namespace Service.ActiveOrders.Jobs
{

    public interface ICleanupDatabaseJob
    {
        void SetLastReceiveTime(DateTime time);
    }

    public class CleanupDatabaseJob: ICleanupDatabaseJob, IDisposable
    {
        private readonly ILogger<CleanupDatabaseJob> _logger;
        private readonly DbContextOptionsBuilder<ActiveOrdersContext> _dbContextOptionsBuilder;
        private DateTime _startTime = DateTime.MinValue;
        private DateTime _lastReceiveTime = DateTime.MinValue;
        private readonly TimeSpan _timeout;

        private readonly MyTaskTimer _timer;

        public CleanupDatabaseJob(ILogger<CleanupDatabaseJob> logger, DbContextOptionsBuilder<ActiveOrdersContext> dbContextOptionsBuilder)
        {
            _timeout = TimeSpan.Parse(Program.Settings.CleanupOrderLastUpdateTimeout);
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _timer = new MyTaskTimer(nameof(CleanupDatabaseJob), _timeout, logger,
                DoTime);
        }

        private async Task DoTime()
        {
            if (_startTime == DateTime.MinValue || _lastReceiveTime == DateTime.MinValue)
            {
                _logger.LogInformation("CleanupDatabaseJob - SKIP - do not started");
                return;
            }

            if ((DateTime.UtcNow - _startTime).TotalMilliseconds < _timeout.TotalMilliseconds * 3)
            {
                _logger.LogInformation("CleanupDatabaseJob - SKIP - wait 3 timeouts after start");
                return;
            }

            if ((DateTime.UtcNow - _lastReceiveTime).TotalMilliseconds < _timeout.TotalMilliseconds / 2)
            {
                _logger.LogInformation($"CleanupDatabaseJob - SKIP - do not receive events (last receive: {_lastReceiveTime:O}");
                return;
            }

            using var action = MyTelemetry.StartActivity("Cleanup database");

            await using var ctx = new ActiveOrdersContext(_dbContextOptionsBuilder.Options, null);

            var count = await ctx.ClearNotActiveOrders(_timeout);

            action?.AddTag("count", count);

            _logger.LogInformation("CleanupDatabaseJob - Done - delete {count} items", count);
        }

        public void SetLastReceiveTime(DateTime time)
        {
            if (_startTime == DateTime.MinValue)
                _startTime = DateTime.UtcNow;

            _lastReceiveTime = time;
        }

        public void Start()
        {
            _timer.Start();
        }


        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}