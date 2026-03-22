using System;

namespace TrackableData
{
    public static class TrackerResolver
    {
        public static Type? GetDefaultTracker<T>() => GetDefaultTracker(typeof(T));

        public static Type? GetDefaultTracker(Type trackableType)
        {
            var pocoType = TrackableResolver.GetPocoType(trackableType);
            if (pocoType != null)
                return typeof(TrackablePocoTracker<>).MakeGenericType(pocoType);

            var containerType = TrackableResolver.GetContainerType(trackableType);
            if (containerType != null)
            {
                var trackerTypeName = containerType.Namespace + "." +
                                      "Trackable" + containerType.Name.Substring(1) + "Tracker";
                return containerType.Assembly.GetType(trackerTypeName);
            }

            if (trackableType.IsGenericType)
            {
                var genericType = trackableType.GetGenericTypeDefinition();

                if (genericType == typeof(TrackableDictionary<,>))
                    return typeof(TrackableDictionaryTracker<,>).MakeGenericType(trackableType.GetGenericArguments());

                if (genericType == typeof(TrackableSet<>))
                    return typeof(TrackableSetTracker<>).MakeGenericType(trackableType.GetGenericArguments());

                if (genericType == typeof(TrackableList<>))
                    return typeof(TrackableListTracker<>).MakeGenericType(trackableType.GetGenericArguments());
            }

            return null;
        }

        public static ITracker? CreateDefaultTracker<T>() => CreateDefaultTracker(typeof(T));

        public static ITracker? CreateDefaultTracker(Type trackableType)
        {
            var trackerType = GetDefaultTracker(trackableType);
            return trackerType != null ? (ITracker)Activator.CreateInstance(trackerType)! : null;
        }
    }
}
