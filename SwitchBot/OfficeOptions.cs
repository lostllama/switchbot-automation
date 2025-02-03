
namespace SwitchBot
{
    public class OfficeOptions
    {
        public required string HubId { get; init; }
        public required string HeaterId { get; init; }
        public required string PlugId { get; init; }
        public required string StateFile { get; set; }
    }
}
