using System.Runtime.Serialization;
using Service.ActiveOrders.Domain.Models;

namespace Service.ActiveOrders.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}