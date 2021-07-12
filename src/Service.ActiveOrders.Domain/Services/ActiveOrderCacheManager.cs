using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.GrpcDataWriter;
using Service.ActiveOrders.Domain.Models;

namespace Service.ActiveOrders.Domain.Services
{
    public class ActiveOrderCacheManager : IActiveOrderCacheManager
    {
        private readonly MyNoSqlGrpcDataWriter _writer;
        private readonly ILogger<ActiveOrderCacheManager> _logger;


        public ActiveOrderCacheManager(
            MyNoSqlGrpcDataWriter writer, 
            ILogger<ActiveOrderCacheManager> logger)
        {
            _writer = writer;
            _logger = logger;
        }

        //todo: add metrics to this method
        public async Task UpdateOrderInNoSqlCache(List<OrderNoSqlEntity> updates)
        {
            using var a1 = MyTelemetry.StartActivity("Update active orders in MyNoSql")?.AddTag("count updates", updates.Count);

            var ordersToDelete = updates.Where(e => e.Order.Status != OrderStatus.Placed).ToList();

            var ordersToUpdate = updates
                .GroupBy(e => e.Order.OrderId)
                .Select(e => e.OrderByDescending(o => o.Order.LastSequenceId).First())
                .Where(e => e.Order.Status == OrderStatus.Placed)
                .ToList();


            var transaction = _writer.BeginTransaction();



            foreach (var group in ordersToDelete.GroupBy(e => e.PartitionKey))
            {
                transaction.DeleteRows(OrderNoSqlEntity.TableName, group.Key, group.Select(e => e.RowKey).ToArray());
            }

            transaction.InsertOrReplaceEntities(ordersToUpdate);

            var sw = new Stopwatch();
            sw.Start();
            await transaction.CommitAsync();
            sw.Stop();

            _logger.LogDebug("[NoSql] Successfully update {countUpdate} items, delete {countDelete}. NoSql time: {timeText} ms", 
                ordersToUpdate.Count, ordersToDelete.Count, sw.ElapsedMilliseconds);
        }

        public async Task<List<SpotOrder>> GetOrdersByWallet(string walletId)
        {
            using var a1 = MyTelemetry.StartActivity("Get active orders by wallet MyNoSql")?.AddTag("walletId", walletId);

            var data = await _writer.GetRowsAsync<OrderNoSqlEntity>(OrderNoSqlEntity.GeneratePartitionKey(walletId)).ToListAsync();

            return data.Select(e => e.Order).ToList();
        }
    }
}