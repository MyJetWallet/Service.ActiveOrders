using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.Domain.Orders;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Grpc;
using Service.ActiveOrders.Grpc.Models;

namespace Service.ActiveOrders.Client
{
    public class ActiveOrderServiceCached : IActiveOrderService
    {
        private readonly IMyNoSqlServerDataReader<OrderNoSqlEntity> _reader;

        public ActiveOrderServiceCached(MyNoSqlReadRepository<OrderNoSqlEntity> reader)
        {
            _reader = reader;
        }

        public Task<SpotOrderList> GetActiveOrdersAsync(GetActiveOrdersRequest request)
        {
            var data = _reader.Get(OrderNoSqlEntity.GeneratePartitionKey(request.WalletId));
            if (data!= null && data.Any())
            {
                var res = data
                    .Where(e => e.IsReal)
                    .Select(e => e.Order)
                    .ToList();

                return Task.FromResult(new SpotOrderList() {Orders = res});
            }
            
            return Task.FromResult(new SpotOrderList() { Orders = new List<SpotOrder>() });
        }
    }
}