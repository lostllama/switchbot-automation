namespace SwitchBot.Models
{
    public class BeforeAfterModel<T>
    {
        public T? Before { get; init; }
        public required T After { get; init; }
    }
}
