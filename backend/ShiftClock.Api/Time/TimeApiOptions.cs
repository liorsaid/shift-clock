namespace ShiftClock.Api.Time;

public sealed class TimeApiOptions
{
    public const string SectionName = "TimeApi";

    public string Url { get; set; } = "";

    public TimeSpan Timeout { get; set; }
}
