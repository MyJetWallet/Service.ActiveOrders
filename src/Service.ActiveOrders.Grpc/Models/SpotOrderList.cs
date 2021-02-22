using System.Collections.Generic;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.ActiveOrders.Grpc.Models
{
    [DataContract]
    public class SpotOrderList
    {
        [DataMember(Order = 1)]
        public List<SpotOrder> Orders { get; set; }
    }
}