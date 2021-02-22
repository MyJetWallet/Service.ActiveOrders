using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Postgres;

namespace Service.ActiveOrders.Postgres.DesignTime
{
    public class ContextFactory : MyDesignTimeContextFactory<ActiveOrdersContext>
    {
        public ContextFactory() : base(options => new ActiveOrdersContext(options, null))
        {
        }
    }
}