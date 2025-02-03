using System.Text.Json.Serialization;

namespace SwitchBot.SwitchBotModels
{
    public class SwitchBotResponse<TBody>
        where TBody : class
    {
        [JsonPropertyName("statusCode")]
        public required int StatusCode { get; init; }
        [JsonPropertyName("message")]
        public required string Message { get; init; }
        [JsonPropertyName("body")]
        public required TBody Body { get; init; }
        public bool IsSuccess => StatusCode == 100;
    }
}
