using SimpleTrading.SettingsReader;

namespace Service.MatchingEngine.PriceSource.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("MatchingEnginePriceSource.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("MatchingEnginePriceSource.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("MatchingEnginePriceSource.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }
    }
}