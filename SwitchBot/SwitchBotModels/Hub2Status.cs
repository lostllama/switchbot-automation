using System.Text.Json.Serialization;

namespace SwitchBot.SwitchBotModels
{
    public class Hub2Status : DeviceStatus
    {
        [JsonPropertyName("temperature")]
        public required float Temperature { get; init; }
        [JsonPropertyName("lightLevel")]
        public required int LightLevel { get; init; }
        [JsonPropertyName("humidity")]
        public required int Humidity { get; init; }
    }
}
