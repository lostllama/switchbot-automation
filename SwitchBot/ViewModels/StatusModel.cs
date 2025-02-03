namespace SwitchBot.ViewModels
{
    public class StatusModel
    {
        public bool IsHeaterOn { get; set; }
        public bool ServiceEnabled { get; set; }
        public float MinTemperature { get; set; }
        public float MaxTemperature { get; set; }
        public float CurrentTemperature { get; set; }
    }
}
