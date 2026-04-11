namespace TrackableData
{
    public interface ITrackableLogger
    {
        void LogDebug(string message, params object?[] args);
    }

    public class NullTrackableLogger : ITrackableLogger
    {
        public static readonly NullTrackableLogger Instance = new NullTrackableLogger();

        public void LogDebug(string message, params object?[] args)
        {
        }
    }
}
