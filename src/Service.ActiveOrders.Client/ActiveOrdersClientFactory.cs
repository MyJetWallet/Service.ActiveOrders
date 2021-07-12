using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using MyJetWallet.Sdk.GrpcMetrics;
using MyNoSqlServer.DataReader;
using ProtoBuf.Grpc.Client;
using Service.ActiveOrders.Domain.Models;
using Service.ActiveOrders.Grpc;

namespace Service.ActiveOrders.Client
{
    [UsedImplicitly]
    public class ActiveOrdersClientFactory
    {
        private readonly CallInvoker _channel;

        public ActiveOrdersClientFactory(string activeOrderGrpcServiceUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(activeOrderGrpcServiceUrl);
            _channel = channel.Intercept(new PrometheusMetricsInterceptor());
        }

        public IActiveOrderService ActiveOrderServiceGrpc() => _channel.CreateGrpcService<IActiveOrderService>();
    }
}
