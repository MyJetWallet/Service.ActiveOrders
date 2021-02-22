using MyJetWallet.Domain.Orders;
using MyNoSqlServer.Abstractions;

namespace Service.ActiveOrders.Domain.Models
{
    public class OrderNoSqlEntity: MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-client-active-orders";

        public const string NoneRowKey = "--none--";

        public static string GeneratePartitionKey(string walletId) => walletId;
        public static string GenerateRowKey(string orderId) => orderId;

        public SpotOrder Order { get; set; }

        public static OrderNoSqlEntity Create(string walletId, SpotOrder order)
        {
            return new OrderNoSqlEntity()
            {
                PartitionKey = GeneratePartitionKey(walletId),
                RowKey = GenerateRowKey(order.OrderId),
                Order = order
            };
        }

        public static OrderNoSqlEntity None(string walletId)
        {
            return new OrderNoSqlEntity()
            {
                PartitionKey = GeneratePartitionKey(walletId),
                RowKey = GenerateRowKey(NoneRowKey),
                Order = new SpotOrder
                {
                    OrderId = NoneRowKey
                }
            };
        }

        public bool IsReal => RowKey != NoneRowKey;
        public bool IsNone => RowKey == NoneRowKey;
    }
}