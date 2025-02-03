using System.Text.Json.Serialization;

namespace SwitchBot.SwitchBotModels
{
    public class DeviceStatus
    {
        [JsonPropertyName("version")]
        public required string Version { get; init; }
        [JsonPropertyName("deviceId")]
        public required string DeviceId { get; init; }
        [JsonPropertyName("deviceType")]
        public required string DeviceType { get; init; }
        [JsonPropertyName("hubDeviceId")]
        public required string HubDeviceId { get; init; }
    }
}
