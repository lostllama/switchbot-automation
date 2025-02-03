using System.ComponentModel.DataAnnotations;

namespace SwitchBot
{
    public class SwitchBotOptions
    {
        public required string Token { get; init; }
        public required string Secret { get; init; }
    }
}
