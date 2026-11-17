namespace ShiftClock.Api.Time;

public sealed class TimeSourceUnavailableException(string reason, Exception? innerException = null)
    : Exception($"Time source unavailable: {reason}", innerException);
