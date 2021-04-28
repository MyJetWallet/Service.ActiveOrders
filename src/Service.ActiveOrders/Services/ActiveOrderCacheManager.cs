using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Postgres;

namespace Service.ActiveOrders.Services
{
    public class ActiveOrderCacheManager : IActiveOrderCacheManager
    {
        private readonly IMyNoSqlServerDataWriter<OrderNoSqlEntity> _writer;
        private readonly DbContextOptionsBuilder<ActiveOrdersContext> _dbContextOptionsBuilder;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ActiveOrderCacheManager> _logger;


        public ActiveOrderCacheManager(IMyNoSqlServerDataWriter<OrderNoSqlEntity> writer, DbContextOptionsBuilder<ActiveOrdersContext> dbContextOptionsBuilder,
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

            await using var transaction = await _writer.BeginTransactionAsync();

            var countInBatch = 0;

            foreach (var wallet in updates.GroupBy(e => e.WalletId))
            {
                if (!await IsWalletExistInCache(wallet.Key))
                {
                    await AddWalletToCache(wallet.Key);
                }
                else
                {
                    var data =
                        wallet
                            .Where(e => e.Status == OrderStatus.Placed)
                            .Select(e => OrderNoSqlEntity.Create(e.WalletId, e));

                    transaction. InsertOrReplace(data);

                    var toDelete = wallet
                        .Where(e => e.Status != OrderStatus.Placed)
                        .Select(e => OrderNoSqlEntity.GenerateRowKey(e.OrderId))
                        .ToArray();

                    transaction.DeleteRows(OrderNoSqlEntity.GeneratePartitionKey(wallet.Key), toDelete);
                }

                countInBatch += wallet.Count();
                if (countInBatch > 1000)
                {
                    _logger.LogDebug("[NoSql] Will post transaction data because collect {count} orders in batch", countInBatch);
                    await transaction.PostAsync();
                    countInBatch = 0;
                }
            }

            using var _ = MyTelemetry.StartActivity("No sql transaction commit");
            await transaction.CommitAsync();

            _logger.LogDebug("[NoSql] Successfully insert or update or delete {count} items", updates.Count);
        }

        public async ValueTask<bool> IsWalletExistInCache(string walletId)
        {
            var entity = await _writer.GetAsync(OrderNoSqlEntity.GeneratePartitionKey(walletId), OrderNoSqlEntity.NoneRowKey);

            return entity != null;
        }

        public async ValueTask<List<OrderNoSqlEntity>> AddWalletToCache(string walletId)
        {
            walletId.AddToActivityAsTag("walletId");

            await using var ctx = GetDbContext();

            await using var transaction = await _writer.BeginTransactionAsync();
            
            var orders = ctx.ActiveOrders.Where(e => e.WalletId == walletId);

            var entityList = await orders.Select(e => OrderNoSqlEntity.Create(e.WalletId, e)).ToListAsync();
            entityList.Add(OrderNoSqlEntity.None(walletId));


            transaction.InsertOrReplace(entityList);

            await transaction.CommitAsync();

            return entityList;
        }

        private ActiveOrdersContext GetDbContext()
        {
            return new ActiveOrdersContext(_dbContextOptionsBuilder.Options, _loggerFactory);
        }
    }
}