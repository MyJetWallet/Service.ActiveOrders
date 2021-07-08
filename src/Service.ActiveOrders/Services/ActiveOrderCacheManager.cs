using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.GrpcDataWriter;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Postgres;

namespace Service.ActiveOrders.Services
{
    public class ActiveOrderCacheManager : IActiveOrderCacheManager
    {
        private readonly MyNoSqlGrpcDataWriter _writer;
        private readonly DbContextOptionsBuilder<ActiveOrdersContext> _dbContextOptionsBuilder;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ActiveOrderCacheManager> _logger;


        public ActiveOrderCacheManager(
            MyNoSqlGrpcDataWriter writer, 
            DbContextOptionsBuilder<ActiveOrdersContext> dbContextOptionsBuilder,
            ILoggerFactory loggerFactory)
        {
            _writer = writer;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ActiveOrderCacheManager>();

            _writer.SetTableMaxPartitionsAmountAsync(OrderNoSqlEntity.TableName, Program.Settings.MaxClientInCache).GetAwaiter().GetResult();
        }


        //todo: add metrics to this method
        public async Task UpdateOrderInNoSqlCache(List<OrderEntity> updates)
        {
            using var a1 = MyTelemetry.StartActivity("Update active orders in MyNoSql")?.AddTag("count updates", updates.Count);

            var taskList = new List<Task>();

            foreach (var wallet in updates.Select(e => e.WalletId).Distinct())
            {
                taskList.Add(AddWalletToCache(wallet));
            }

            await Task.WhenAll(taskList);
        }

        public async Task<List<OrderNoSqlEntity>> AddWalletToCache(string walletId)
        {
            var sw = new Stopwatch();
            sw.Start();

            using var activity = MyTelemetry.StartActivity("Add wallet to cache");
            walletId.AddToActivityAsTag("walletId");
            
            var entities = await LoadWallet(walletId);

            var transaction = _writer.BeginTransaction();

            transaction.DeletePartitions(OrderNoSqlEntity.TableName, new []{ OrderNoSqlEntity.GeneratePartitionKey(walletId) });

            transaction.InsertOrReplaceEntities(entities);

            var sw1 = new Stopwatch();
            using (var _ = MyTelemetry.StartActivity("No sql transaction commit"))
            {
                sw1.Start();
                await transaction.CommitAsync();
                sw1.Stop();
            }

            sw.Stop();

            _logger.LogDebug("[NoSql] Successfully update {count} items. Time: {timeText} ms, NoSql time: {timeText2}, Wallet: {walletId}", entities.Count(), sw.ElapsedMilliseconds.ToString(), sw1.ElapsedMilliseconds.ToString(), walletId);

            return entities;
        }

        public async Task<List<OrderNoSqlEntity>> LoadWallet(string walletId)
        {
            using var activity = MyTelemetry.StartActivity("Load wallet from database");
            walletId.AddToActivityAsTag("walletId");

            await using var ctx = GetDbContext();
            
            var orders = ctx.ActiveOrders.Where(e => e.WalletId == walletId && e.Status == OrderStatus.Placed);
            var entityList = await orders.Select(e => OrderNoSqlEntity.Create(e.WalletId, e)).ToListAsync();

            entityList.Count.AddToActivityAsTag("count-item");

            return entityList;
        }

        private ActiveOrdersContext GetDbContext()
        {
            return new ActiveOrdersContext(_dbContextOptionsBuilder.Options, _loggerFactory);
        }
    }
}