using MyJetWallet.Domain.Orders;

namespace Service.ActiveOrders.Postgres
{
    public class OrderEntity : SpotOrder
    {
        public string WalletId { get; set; }

        public string BrokerId { get; set; }

        public string ClientId { get; set; }

        public static OrderEntity Create(string walletId, string brokerId, string clientId, SpotOrder order)
        {
            return new OrderEntity()
            {
                BrokerId = brokerId,
                ClientId = clientId,
                WalletId = walletId,
                OrderId = order.OrderId,
                LastUpdate = order.LastUpdate,
                LastSequenceId = order.LastSequenceId,
                RemainingVolume = order.RemainingVolume,
                Status = order.Status,
                Volume = order.Volume,
                Price = order.Price,
                CreatedTime = order.CreatedTime,
                InstrumentSymbol = order.InstrumentSymbol,
                Side = order.Side,
                Type = order.Type
            };
        }
    }
}