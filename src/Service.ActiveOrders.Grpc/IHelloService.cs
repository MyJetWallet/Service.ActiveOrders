using System.ServiceModel;
using System.Threading.Tasks;
using Service.ActiveOrders.Grpc.Models;

namespace Service.ActiveOrders.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}