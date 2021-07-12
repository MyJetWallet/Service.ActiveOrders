using System.Collections.Generic;
using System.Threading.Tasks;
using MyJetWallet.Domain.Orders;
using Service.ActiveOrders.Domain.Models;

namespace Service.ActiveOrders.Domain.Services
{
    public interface IActiveOrderCacheManager
    {
        Task UpdateOrderInNoSqlCache(List<OrderNoSqlEntity> updates);

        Task<List<SpotOrder>> GetOrdersByWallet(string walletId);
    }
}