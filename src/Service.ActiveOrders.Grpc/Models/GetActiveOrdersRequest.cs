using System.Runtime.Serialization;

namespace Service.ActiveOrders.Grpc.Models
{
    [DataContract]
    public class GetActiveOrdersRequest
    {
        [DataMember(Order = 1)] public string WalletId { get; set; }
    }
}