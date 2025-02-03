using System.Text.Json.Serialization;

namespace SwitchBot.SwitchBotModels
{
    public class PlugMiniStatus : DeviceStatus
    {
        [JsonPropertyName("power")]
        public required string Power { get; init; }

        [JsonPropertyName("voltage")]
        public required float Voltage { get; init; }

        [JsonPropertyName("weight")]
        public required float Weight { get; init; }

        [JsonPropertyName("electricityOfDay")]
        public required int ElectricityOfDay { get; init; }

        [JsonPropertyName("electricCurrent")]
        public required float ElectricCurrent { get; init; }
    }
}
