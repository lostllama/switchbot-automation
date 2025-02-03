using System;

namespace SwitchBot.Models
{
    public class PlugModel
    {
        public required PlugStatus Status { get; init; }
        public required float Voltage { get; init; }
        public required float Current { get; init; }
        public float Wattage => Voltage * Current;
        public float DayPowerConsumptionWatts { get; init; }
        public TimeSpan DayUsageDuration { get; init; }
    }
}
