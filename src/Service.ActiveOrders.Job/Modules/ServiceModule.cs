using Autofac;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.GrpcDataWriter;
using MyServiceBus.Abstractions;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Domain.Services;
using Service.ActiveOrders.Job.Jobs;
using Service.MatchingEngine.EventBridge.ServiceBus;

namespace Service.ActiveOrders.Job.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(
                Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName,
                Program.LoggerFactory);
            
            builder.RegisterMeEventSubscriber(serviceBusClient, "active-orders-cache", TopicQueueType.PermanentWithSingleConnection);

            builder
                .RegisterType<ActiveOrdersUpdateJob>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<ActiveOrderCacheManager>()
                .As<IActiveOrderCacheManager>()
                .SingleInstance();

            var noSqlWriter = MyNoSqlGrpcDataWriterFactory
                .CreateNoSsl(Program.Settings.MyNoSqlWriterGrpc)
                .RegisterSupportedEntity<OrderNoSqlEntity>(OrderNoSqlEntity.TableName);

            builder.RegisterInstance(noSqlWriter).AsSelf().SingleInstance();

        }
    }
}