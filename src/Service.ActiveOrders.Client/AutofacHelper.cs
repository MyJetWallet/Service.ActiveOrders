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
        public static void RegisterActiveOrdersClients(this ContainerBuilder builder, string balancesGrpcServiceUrl, IMyNoSqlSubscriber myNoSqlSubscriber)
        {
            var subs = new MyNoSqlReadRepository<OrderNoSqlEntity>(myNoSqlSubscriber, OrderNoSqlEntity.TableName);

            var factory = new ActiveOrdersClientFactory(balancesGrpcServiceUrl, subs);

            builder.RegisterInstance(factory.ActiveOrderService()).As<IActiveOrderService>().SingleInstance();

            builder.RegisterInstance(subs).As<IMyNoSqlServerDataReader<OrderNoSqlEntity>>().SingleInstance();
        }
    }
}