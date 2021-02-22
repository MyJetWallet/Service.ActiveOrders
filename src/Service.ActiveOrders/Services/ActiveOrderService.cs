using System;
using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.Domain.Orders;
using Service.ActiveOrders.Grpc;
using Service.ActiveOrders.Grpc.Models;

namespace Service.ActiveOrders.Services
{
    public class ActiveOrderService: IActiveOrderService
    {
        private readonly IActiveOrderCacheManager _cacheManager;

        public ActiveOrderService(IActiveOrderCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public async Task<SpotOrderList> GetActiveOrdersAsync(GetActiveOrdersRequest request)
        {
            var data = await _cacheManager.AddWalletToCache(request.WalletId);

            return new SpotOrderList()
            {
                Orders = data
                    .Where(e => e.IsReal)
                    .Select(e => new SpotOrder(
                        e.Order.OrderId,
                        e.Order.Type,
                        e.Order.InstrumentSymbol,
                        e.Order.Side,
                        e.Order.Price,
                        e.Order.Volume,
                        e.Order.RemainingVolume,
                        e.Order.CreatedTime,
                        e.Order.LastUpdate,
                        e.Order.Status,
                        e.Order.LastSequenceId))
                    .ToList()
            };
        }
    }
}