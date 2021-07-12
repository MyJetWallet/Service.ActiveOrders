using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Domain.Services;

namespace Service.ActiveOrders.Job.Jobs
{
    public class ActiveOrdersUpdateJob
    {
        private readonly IActiveOrderCacheManager _cacheCacheManager;
        private readonly ILogger<ActiveOrdersUpdateJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private int _maxBatchSize;

        public ActiveOrdersUpdateJob(ISubscriber<IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent>> subscriber,
            IActiveOrderCacheManager cacheCacheManager,
            ILogger<ActiveOrdersUpdateJob> logger,
            ILoggerFactory loggerFactory)
        {
            _cacheCacheManager = cacheCacheManager;
            _logger = logger;
            _loggerFactory = loggerFactory;

            _maxBatchSize = Program.Settings.MaxUpdateBatchSize;

            subscriber.Subscribe(HandleEvents);
        }

        private async ValueTask HandleEvents(IReadOnlyList<ME.Contracts.OutgoingMessages.OutgoingEvent> events)
        {
            using var activity = MyTelemetry.StartActivity("Handle ME OutgoingEvent's")?.AddTag("count-events", events.Count);

            _logger.LogInformation("Receive {count} events", events.Count);
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var updates = events
                    .SelectMany(e => e.Orders.Select(u => new {Update = u, e.Header.SequenceNumber, e.Header.Timestamp}))
                    .GroupBy(e => new {e.Update.WalletId, e.Update.Id})
                    .Select(e => e.OrderByDescending(i => i.SequenceNumber).First())
                    .Select(e =>
                    {
                        var id = e.Update.ExternalId;
                        var volume = !string.IsNullOrEmpty(e.Update.Volume) ? e.Update.Volume : "0";
                        var price = !string.IsNullOrEmpty(e.Update.Price) ?  e.Update.Price : "0";
                        var remainingVolume = !string.IsNullOrEmpty(e.Update.RemainingVolume) ? e.Update.RemainingVolume : "0";

                        var entity =  OrderNoSqlEntity.Create(
                            e.Update.WalletId,
                            new SpotOrder(
                                id,
                                MapOrderType(e.Update.OrderType),
                                e.Update.AssetPairId,
                                MapSide(e.Update.Side),
                                double.Parse(price),
                                double.Parse(volume),
                                double.Parse(remainingVolume),
                                e.Update.Registered.ToDateTime(),
                                DateTime.UtcNow, 
                                MapStatus(e.Update.Status),
                                e.SequenceNumber
                            ));
                        return entity;
                    })
                    .OrderBy(e => e.Order.LastSequenceId)
                    .ToList();


                var index = 0;

                while (index < updates.Count)
                {
                    var orders = updates.Skip(index).Take(_maxBatchSize).ToList();

                    _logger.LogInformation("Take {count} orders from batch {catchCount}", orders.Count, updates.Count);

                    try
                    {
                        await _cacheCacheManager.UpdateOrderInNoSqlCache(updates);
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Cannot handle {count}order updates", orders.Count);
                        if (_maxBatchSize > 50)
                        {
                            _maxBatchSize = _maxBatchSize / 2;
                            Console.WriteLine($"Batch size decreased to {_maxBatchSize}");
                        }

                        throw;
                    }

                    index += orders.Count;
                }

                if (_maxBatchSize != Program.Settings.MaxUpdateBatchSize)
                {
                    _maxBatchSize = Program.Settings.MaxUpdateBatchSize;
                    Console.WriteLine($"Batch size restored to {_maxBatchSize}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot handle batch of MeEvent's");
                ex.FailActivity();

                await Task.Delay(500);

                throw;
            }

            sw.Stop();
            _logger.LogInformation("Handled {count} events. Time: {timeRangeText}", events.Count, sw.Elapsed.ToString());
        }

        

        private OrderType MapOrderType(ME.Contracts.OutgoingMessages.Order.Types.OrderType orderType)
        {
            switch (orderType)
            {
                case ME.Contracts.OutgoingMessages.Order.Types.OrderType.Limit:
                    return OrderType.Limit;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderType.Market:
                    return OrderType.Market;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderType.StopLimit:
                    return OrderType.StopLimit;
            }

            Console.WriteLine($"Receive unknown order type from ME: {orderType}");
            return OrderType.UnknownOrderType;
        }

        private OrderSide MapSide(ME.Contracts.OutgoingMessages.Order.Types.OrderSide side)
        {
            switch (side)
            {
                case ME.Contracts.OutgoingMessages.Order.Types.OrderSide.Buy:
                    return OrderSide.Buy;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderSide.Sell:
                    return OrderSide.Sell;
            }

            Console.WriteLine($"Receive unknown order side from ME: {side}");
            return OrderSide.UnknownOrderSide;
        }

        private OrderStatus MapStatus(ME.Contracts.OutgoingMessages.Order.Types.OrderStatus status)
        {
            switch (status)
            {
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Placed:
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.PartiallyMatched:
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Replaced:
                    return OrderStatus.Placed;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Matched:
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Executed:
                    return OrderStatus.Filled;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Cancelled:
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Rejected:
                    return OrderStatus.Cancelled;

                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.Pending:
                case ME.Contracts.OutgoingMessages.Order.Types.OrderStatus.UnknownStatus:
                    Console.WriteLine($"Receive unknown status from ME: {status}");
                    return OrderStatus.UnknownStatus;
            }

            Console.WriteLine($"Receive unknown status from ME: {status}");
            return OrderStatus.UnknownStatus;
        }
    }
}