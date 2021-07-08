using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.GrpcDataWriter;
using MyServiceBus.Abstractions;
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
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(
                Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName,
                Program.LoggerFactory);
            
            builder.RegisterMeEventSubscriber(serviceBusClient, "active-orders", TopicQueueType.Permanent);

            builder
                .RegisterType<ActiveOrdersUpdateJob>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<ActiveOrderCacheManager>()
                .As<IActiveOrderCacheManager>()
                .SingleInstance();

            builder
                .RegisterType<CleanupDatabaseJob>()
                .As<ICleanupDatabaseJob>()
                .AsSelf()
                .SingleInstance();

            //builder.RegisterMyNoSqlWriter<OrderNoSqlEntity>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), OrderNoSqlEntity.TableName);

            var noSqlWriter = MyNoSqlGrpcDataWriterFactory
                .CreateNoSsl(Program.Settings.MyNoSqlWriterGrpc)
                .RegisterSupportedEntity<OrderNoSqlEntity>(OrderNoSqlEntity.TableName);

            builder.RegisterInstance(noSqlWriter).AsSelf().SingleInstance();
        }
    }
}