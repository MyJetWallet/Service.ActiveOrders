using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.ActiveOrders.Domain.Settings
{
    public class SettingsModel
    {
        [YamlProperty("ActiveOrders.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ActiveOrders.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("ActiveOrders.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("ActiveOrders.MaxClientInCache")]
        public int MaxClientInCache { get; set; }

        [YamlProperty("ActiveOrders.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ActiveOrders.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("ActiveOrders.MyNoSqlWriterGrpc")]
        public string MyNoSqlWriterGrpc { get; set; }

        [YamlProperty("ActiveOrders.CleanupOrderLastUpdateTimeout")]
        public string CleanupOrderLastUpdateTimeout { get; set; }

        [YamlProperty("ActiveOrders.MaxUpdateBatchSize")]
        public int MaxUpdateBatchSize { get; set; }
    }
}