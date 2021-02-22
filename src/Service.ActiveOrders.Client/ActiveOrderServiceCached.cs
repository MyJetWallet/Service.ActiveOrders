using System.Linq;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Grpc;
using Service.ActiveOrders.Grpc.Models;

namespace Service.ActiveOrders.Client
{
    public class ActiveOrderServiceCached : IActiveOrderService
    {
        private readonly IActiveOrderService _service;
        private readonly IMyNoSqlServerDataReader<OrderNoSqlEntity> _reader;

        public ActiveOrderServiceCached(IActiveOrderService service, MyNoSqlReadRepository<OrderNoSqlEntity> reader)
        {
            _service = service;
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

            return _service.GetActiveOrdersAsync(request);
        }
    }
}