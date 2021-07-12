using Autofac;
using MyNoSqlServer.GrpcDataWriter;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Domain.Services;

namespace Service.ActiveOrders.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var noSqlWriter = MyNoSqlGrpcDataWriterFactory
                .CreateNoSsl(Program.Settings.MyNoSqlWriterGrpc)
                .RegisterSupportedEntity<OrderNoSqlEntity>(OrderNoSqlEntity.TableName);

            builder.RegisterInstance(noSqlWriter).AsSelf().SingleInstance();

            builder
                .RegisterType<ActiveOrderCacheManager>()
                .As<IActiveOrderCacheManager>()
                .SingleInstance();
        }
    }
}