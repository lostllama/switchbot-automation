namespace SwitchBot.Models
{
    public class ConditionsModel
    {
        public required float Temperature { get; init; }
        public required int Humidity { get; init; }
        public int? LightLevel { get; init; }
    }
}
