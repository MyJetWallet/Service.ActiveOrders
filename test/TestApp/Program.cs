using System;
using System.Threading.Tasks;
using MyNoSqlServer.DataReader;
using Newtonsoft.Json;
using ProtoBuf.Grpc.Client;
using Service.ActiveOrders.Client;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Press enter to start");
            Console.ReadLine();


            var myNoSqlClient = new MyNoSqlTcpClient(() => "192.168.10.80:5125", "Test-app");

            var subs = new MyNoSqlReadRepository<OrderNoSqlEntity>(myNoSqlClient, OrderNoSqlEntity.TableName);


            myNoSqlClient.Start();
            await Task.Delay(2000);
            var factory = new ActiveOrdersClientFactory("http://localhost:80", subs);

            var client = factory.ActiveOrderService();

            var orders = await client.GetActiveOrdersAsync(new GetActiveOrdersRequest(){WalletId = "manual-test-w-003" });

            Console.WriteLine(JsonConvert.SerializeObject(orders, Formatting.Indented));
            

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("-------------------");
                Console.WriteLine();

                orders = await client.GetActiveOrdersAsync(new GetActiveOrdersRequest() { WalletId = "manual-test-w-003" });

                Console.WriteLine(JsonConvert.SerializeObject(orders, Formatting.Indented));
                Console.ReadLine();
            }

            




            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
