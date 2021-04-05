using Autofac;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Jobs;
using Service.ActiveOrders.Services;
using Service.MatchingEngine.EventBridge.ServiceBus;

namespace Service.ActiveOrders.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterMyNoSqlWriter<OrderNoSqlEntity>(builder, OrderNoSqlEntity.TableName);


            var serviceBusClient = new MyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName);
            builder.RegisterInstance(serviceBusClient).AsSelf().SingleInstance();
            builder.RegisterMeEventSubscriber(serviceBusClient, "active-orders", TopicQueueType.Permanent);


            builder.RegisterType<ActiveOrdersUpdateJob>().AutoActivate().SingleInstance();

            builder
                .RegisterType<NoSqlCleanupJob>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<ActiveOrderCacheManager>()
                .As<IActiveOrderCacheManager>()
                .SingleInstance();
        }

        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx => new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<TEntity>(
                    Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}