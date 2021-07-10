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
            var data = _cacheManager.GetWalletOrders(request.WalletId);

            return new SpotOrderList()
            {
                Orders = data
                    .Select(e => new SpotOrder(
                        e.OrderId,
                        e.Type,
                        e.InstrumentSymbol,
                        e.Side,
                        e.Price,
                        e.Volume,
                        e.RemainingVolume,
                        e.CreatedTime,
                        e.LastUpdate,
                        e.Status,
                        e.LastSequenceId))
                    .ToList()
            };
        }
    }
}