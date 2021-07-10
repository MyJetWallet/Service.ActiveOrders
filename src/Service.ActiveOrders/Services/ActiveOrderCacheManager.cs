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


        public List<OrderEntity> GetWalletOrders(string walletId)
        {
            Dictionary<string, OrderEntity> walletOrders;

            lock (_data)
            {
                if (!_data.TryGetValue(walletId, out walletOrders))
                {
                    walletOrders = new Dictionary<string, OrderEntity>();
                }
            }

            lock (walletOrders)
            {
                return walletOrders.Values.ToList();
            }
        }

        //todo: add metrics to this method
        public async Task UpdateOrderInNoSqlCache(List<OrderEntity> updates)
        {
            using var a1 = MyTelemetry.StartActivity("Update active orders in MyNoSql")?.AddTag("count updates", updates.Count);

            var taskList = new List<Task>();


            foreach (var group in updates.GroupBy(e => e.WalletId))
            {
                taskList.Add(UpdateWalletInCache(group.Key, group.ToList()));
            }

            await Task.WhenAll(taskList);
        }

        private Dictionary<string, Dictionary<string, OrderEntity>> _data = new Dictionary<string, Dictionary<string, OrderEntity>>();

        private async Task UpdateWalletInCache(string walletId, List<OrderEntity> orders)
        {
            Dictionary<string, OrderEntity> walletOrders;
            lock (_data)
            {
                if (!_data.TryGetValue(walletId, out walletOrders))
                {
                    walletOrders = new Dictionary<string, OrderEntity>();
                    _data[walletId] = walletOrders;
                }
            }

            lock (walletOrders)
            {
                foreach (var order in orders.OrderBy(e => e.LastSequenceId))
                {
                    if (order.Status != OrderStatus.Placed)
                        walletOrders.Remove(order.OrderId);
                    else
                        walletOrders[order.OrderId] = order;
                }
            }

            var transaction = _writer.BeginTransaction();
            var list = walletOrders.Values.Select(e => OrderNoSqlEntity.Create(e.WalletId, e)).ToList();
            transaction.DeletePartitions(OrderNoSqlEntity.TableName, new[] { OrderNoSqlEntity.GeneratePartitionKey(walletId) });
            transaction.InsertOrReplaceEntities(list);

            var sw1 = new Stopwatch();
            using (var _ = MyTelemetry.StartActivity("No sql transaction commit"))
            {
                sw1.Start();
                await transaction.CommitAsync();
                sw1.Stop();
            }

            _logger.LogDebug("[NoSql] Successfully update {count} items. NoSql time: {timeText2}, Wallet: {walletId}", list.Count(), sw1.ElapsedMilliseconds.ToString(), walletId);

        }

        private ActiveOrdersContext GetDbContext()
        {
            return new ActiveOrdersContext(_dbContextOptionsBuilder.Options, _loggerFactory);
        }
    }
}