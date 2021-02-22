using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using MyJetWallet.Domain.Orders;
using Service.ActiveOrders.Grpc.Models;

namespace Service.ActiveOrders.Grpc
{
    [ServiceContract]
    public interface IActiveOrderService
    {
        [OperationContract]
        Task<SpotOrderList> GetActiveOrdersAsync(GetActiveOrdersRequest request);
    }

    public static class ActiveOrderServiceHelper
    {
        public static async Task<List<SpotOrder>> GetActiveOrderByWalletAsync(this IActiveOrderService service, string walletId)
        {
            var data = await service.GetActiveOrdersAsync(new GetActiveOrdersRequest() { WalletId = walletId });
            return data.Orders;
        }
    }
}