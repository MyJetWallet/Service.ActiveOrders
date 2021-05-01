using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
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
        }


        //todo: add metrics to this method
        public async Task UpdateOrderInNoSqlCache(List<OrderEntity> updates)
        {
            using var activity = MyTelemetry.StartActivity("Update active orders in MyNoSql")?.AddTag("count updates", updates.Count);

            foreach (var wallet in updates.GroupBy(e => e.WalletId))
            {
                var transaction = _writer.BeginTransaction();

                if (!await IsWalletExistInCache(wallet.Key))
                {
                    await AddWalletToCache(wallet.Key);
                }
                else
                {
                    var toDelete = wallet
                        .Where(e => e.Status != OrderStatus.Placed)
                        .Select(e => OrderNoSqlEntity.GenerateRowKey(e.OrderId))
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToArray();

                    var data =
                        wallet
                            .Where(e => e.Status == OrderStatus.Placed && !toDelete.Contains(e.OrderId))
                            .Select(e => OrderNoSqlEntity.Create(e.WalletId, e));

                    transaction.InsertOrReplaceEntities(data);

                    transaction.DeleteRows(OrderNoSqlEntity.TableName, OrderNoSqlEntity.GeneratePartitionKey(wallet.Key), toDelete);
                }

                var sw = new Stopwatch();
                using (var _ = MyTelemetry.StartActivity("No sql transaction commit"))
                {
                    sw.Start();
                    await transaction.CommitAsync();
                    sw.Stop();
                }

                _logger.LogDebug("[NoSql] Successfully insert or update or delete {count} items. Time: {timeText} ms, Wallet: {walletId}", wallet.Count(), sw.ElapsedMilliseconds.ToString(), wallet.Key);
            }
        }

        public async ValueTask<bool> IsWalletExistInCache(string walletId)
        {
            var entity = await _writer.GetRowAsync<OrderNoSqlEntity>(OrderNoSqlEntity.GeneratePartitionKey(walletId), OrderNoSqlEntity.NoneRowKey);

            return entity != null;
        }

        public async ValueTask<List<OrderNoSqlEntity>> AddWalletToCache(string walletId)
        {
            walletId.AddToActivityAsTag("walletId");

            await using var ctx = GetDbContext();

            var transaction = _writer.BeginTransaction();
            
            var orders = ctx.ActiveOrders.Where(e => e.WalletId == walletId);

            var entityList = await orders.Select(e => OrderNoSqlEntity.Create(e.WalletId, e)).ToListAsync();
            entityList.Add(OrderNoSqlEntity.None(walletId));


            transaction.InsertOrReplaceEntities(entityList);

            await transaction.CommitAsync();

            return entityList;
        }

        private ActiveOrdersContext GetDbContext()
        {
            return new ActiveOrdersContext(_dbContextOptionsBuilder.Options, _loggerFactory);
        }
    }
}