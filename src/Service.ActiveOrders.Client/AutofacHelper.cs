using Autofac;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Grpc;
// ReSharper disable UnusedMember.Global

namespace Service.ActiveOrders.Client
{
    public static class AutofacHelper
    {
        public static void RegisterActiveOrdersClients(this ContainerBuilder builder, string activeOrdersGrpcServiceUrl, IMyNoSqlSubscriber myNoSqlSubscriber)
        {
            var subs = new MyNoSqlReadRepository<OrderNoSqlEntity>(myNoSqlSubscriber, OrderNoSqlEntity.TableName);

            var service = new ActiveOrderServiceCached(subs);

            builder.RegisterInstance(service).As<IActiveOrderService>().SingleInstance();

            builder.RegisterInstance(subs).As<IMyNoSqlServerDataReader<OrderNoSqlEntity>>().SingleInstance();
        }

        public static void RegisterActiveOrdersClientsWithoutCache(this ContainerBuilder builder, string activeOrdersGrpcServiceUrl)
        {
            var factory = new ActiveOrdersClientFactory(activeOrdersGrpcServiceUrl);

            builder.RegisterInstance(factory.ActiveOrderServiceGrpc()).As<IActiveOrderService>().SingleInstance();
        }
    }
}