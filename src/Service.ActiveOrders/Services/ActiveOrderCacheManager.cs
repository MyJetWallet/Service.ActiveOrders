﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
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

        public ActiveOrderCacheManager(IMyNoSqlServerDataWriter<OrderNoSqlEntity> writer, DbContextOptionsBuilder<ActiveOrdersContext> dbContextOptionsBuilder,
            ILoggerFactory loggerFactory)
        {
            _writer = writer;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _loggerFactory = loggerFactory;
        }


        //todo: add metrics to this method
        public async Task UpdateOrderInNoSqlCache(List<OrderEntity> updates)
        {
            var insertList = new List<OrderNoSqlEntity>();
            var deleteList = new List<(string, string)>();

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

                    insertList.AddRange(data);
                    

                    var toDelete = wallet
                        .Where(e => e.Status != OrderStatus.Placed)
                        .Select(e => (OrderNoSqlEntity.GeneratePartitionKey(e.WalletId), OrderNoSqlEntity.GenerateRowKey(e.OrderId)));

                    deleteList.AddRange(toDelete);
                }

                if (insertList.Any())
                {
                    await _writer.BulkInsertOrReplaceAsync(insertList);
                }

                if (deleteList.Any())
                {
                    foreach (var item in deleteList)
                    {
                        await _writer.DeleteAsync(item.Item1, item.Item2);
                    }
                }
            }
        }

        public async ValueTask<bool> IsWalletExistInCache(string walletId)
        {
            var entity = await _writer.GetAsync(OrderNoSqlEntity.GeneratePartitionKey(walletId), OrderNoSqlEntity.NoneRowKey);

            return entity != null;
        }

        public async ValueTask<List<OrderNoSqlEntity>> AddWalletToCache(string walletId)
        {
            await using var ctx = GetDbContext();

            var orders = ctx.ActiveOrders.Where(e => e.WalletId == walletId);

            var entityList = await orders.Select(e => OrderNoSqlEntity.Create(e.WalletId, e)).ToListAsync();
            entityList.Add(OrderNoSqlEntity.None(walletId));

            await _writer.BulkInsertOrReplaceAsync(entityList);

            return entityList;
        }

        private ActiveOrdersContext GetDbContext()
        {
            return new ActiveOrdersContext(_dbContextOptionsBuilder.Options, _loggerFactory);
        }
    }
}