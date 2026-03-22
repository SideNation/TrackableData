using System.Collections.Generic;

namespace TrackableData
{
    public interface ITrackable
    {
        bool Changed { get; }
        ITracker? Tracker { get; set; }
        ITrackable Clone();
        ITrackable? GetChildTrackable(object name);
        IEnumerable<KeyValuePair<object, ITrackable>> GetChildTrackables(bool changedOnly = false);
    }

    public interface ITrackable<T> : ITrackable
    {
        new ITracker<T>? Tracker { get; set; }
    }

    public interface ITrackablePoco
    {
    }

    public interface ITrackablePoco<T> : ITrackable<T>, ITrackablePoco
    {
        new IPocoTracker<T>? Tracker { get; set; }
    }

    public interface ITrackableContainer
    {
    }

    public interface ITrackableContainer<T> : ITrackable<T>, ITrackableContainer
    {
        new IContainerTracker<T>? Tracker { get; set; }
    }
}
