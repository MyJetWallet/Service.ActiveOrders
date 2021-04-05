using System;
using System.Timers;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.ActiveOrders.Domain.Models;

namespace Service.ActiveOrders.Jobs
{
    public class NoSqlCleanupJob: IStartable, IDisposable
    {
        private readonly IMyNoSqlServerDataWriter<OrderNoSqlEntity> _writer;
        private readonly ILogger<NoSqlCleanupJob> _logger;
        private Timer _timer;

        public NoSqlCleanupJob(IMyNoSqlServerDataWriter<OrderNoSqlEntity> writer, ILogger<NoSqlCleanupJob> logger)
        {
            _writer = writer;
            _logger = logger;
        }

        public void Start()
        {
            _timer = new Timer();
            _timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            _timer.Elapsed += DoTime;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
        }

        private void DoTime(object sender, ElapsedEventArgs e)
        {
            using var activity = MyTelemetry.StartActivity("Clean NoSql");

            try
            {
                var maxClients = Program.ReloadedSettings(e => e.MaxClientInCache).Invoke();
                _writer.CleanAndKeepMaxPartitions(maxClients).GetAwaiter().GetResult();
                _logger.LogInformation($"Cleanup {OrderNoSqlEntity.TableName} is done, keep max {maxClients} clients");
                maxClients.AddToActivityAsTag("max clients");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Cannot cleanup {OrderNoSqlEntity.TableName}");
                ex.FailActivity();
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}