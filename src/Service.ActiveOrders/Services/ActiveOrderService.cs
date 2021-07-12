using System.Threading.Tasks;
using Service.ActiveOrders.Domain.Services;
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
            var data = await _cacheManager.GetOrdersByWallet(request.WalletId);

            return new SpotOrderList()
            {
                Orders = data
            };
        }
    }
}