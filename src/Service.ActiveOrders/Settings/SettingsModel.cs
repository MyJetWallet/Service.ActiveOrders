using SimpleTrading.SettingsReader;

namespace Service.ActiveOrders.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("ActiveOrders.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ActiveOrders.PostgresConnectionString")]
        public string PostgresConnectionString { get; set; }

        [YamlProperty("ActiveOrders.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("ActiveOrders.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("ActiveOrders.MaxClientInCache")]
        public int MaxClientInCache { get; set; }

        [YamlProperty("ActiveOrders.ZipkinUrl")]
        public string ZipkinUrl { get; set; }
    }
}