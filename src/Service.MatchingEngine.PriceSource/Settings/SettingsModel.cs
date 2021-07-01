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

        [YamlProperty("MatchingEnginePriceSource.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.MatchingEngine.OrderBookServiceGrpcUrl")]
        public string OrderBookServiceGrpcUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.ZipkinUrl")]
        public string ZipkinUrl { get; set; }
    }
}