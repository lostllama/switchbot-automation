using System.Text.Json.Serialization;

namespace SwitchBot.SwitchBotModels
{
    public class OutdoorMeterStatus : DeviceStatus
    {
        [JsonPropertyName("temperature")]
        public required float Temperature { get; init; }
        [JsonPropertyName("humidity")]
        public required int Humidity { get; init; }
        [JsonPropertyName("battery")]
        public required int Battery { get; init; }
    }
}
