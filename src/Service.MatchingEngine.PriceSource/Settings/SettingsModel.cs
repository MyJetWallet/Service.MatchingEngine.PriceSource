using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.MatchingEngine.PriceSource.Settings
{
    public class SettingsModel
    {
        [YamlProperty("MatchingEnginePriceSource.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("MatchingEnginePriceSource.CandleServiceBusHostPort")]
        public string CandleServiceBusHostPort { get; set; }

        [YamlProperty("MatchingEnginePriceSource.MatchingEngine.OrderBookServiceGrpcUrl")]
        public string OrderBookServiceGrpcUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.MyNoSqlWriterGrpc")]
        public string MyNoSqlWriterGrpc { get; set; }

        [YamlProperty("MatchingEnginePriceSource.MaxMeEventsBatchSize")]
        public int MaxMeEventsBatchSize { get; set; }

        [YamlProperty("MatchingEnginePriceSource.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
    }
}