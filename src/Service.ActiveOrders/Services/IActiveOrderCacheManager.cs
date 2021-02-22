using System.Collections.Generic;
using System.Threading.Tasks;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Postgres;

namespace Service.ActiveOrders.Services
{
    public interface IActiveOrderCacheManager
    {
        Task UpdateOrderInNoSqlCache(List<OrderEntity> updates);
        ValueTask<List<OrderNoSqlEntity>> AddWalletToCache(string walletId);
    }
}