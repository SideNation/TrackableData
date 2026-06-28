using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public static class TrackableJsonExtensions
    {
        public static string SerializeChangedTrackersWithPath(
            this ITrackable trackable,
            JsonSerializerSettings? jsonSerializerSettings = null)
        {
            if (trackable == null)
                throw new ArgumentNullException(nameof(trackable));

            var settings = jsonSerializerSettings ?? TrackableJsonSerializerSettings.Create();
            var pathToTrackerMap = trackable.GetChangedTrackersWithPath().ToDictionary(x => x.Key, x => x.Value);
            return JsonConvert.SerializeObject(pathToTrackerMap, settings);
        }

        public static void ApplyTo(
            this string json,
            ITrackable trackable,
            JsonSerializerSettings? jsonSerializerSettings = null)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            var settings = jsonSerializerSettings ?? TrackableJsonSerializerSettings.Create();
            var pathToTrackerTokens = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json, settings);
            pathToTrackerTokens?.ApplyTo(trackable, settings);
        }

        public static void ApplyTo(
            this IEnumerable<KeyValuePair<string, JToken>> pathAndTrackerTokens,
            ITrackable trackable,
            JsonSerializerSettings? jsonSerializerSettings = null)
        {
            if (pathAndTrackerTokens == null)
                throw new ArgumentNullException(nameof(pathAndTrackerTokens));

            if (trackable == null)
                throw new ArgumentNullException(nameof(trackable));

            var settings = jsonSerializerSettings ?? TrackableJsonSerializerSettings.Create();
            var serializer = JsonSerializer.Create(settings);
            foreach (var item in pathAndTrackerTokens)
            {
                var targetTrackable = trackable.GetTrackableByPath(item.Key);
                if (targetTrackable == null)
                    continue;

                var trackerType = TrackerResolver.GetDefaultTracker(targetTrackable.GetType());
                if (trackerType == null)
                    continue;

                var tracker = item.Value.ToObject(trackerType, serializer) as ITracker;
                tracker?.ApplyTo(targetTrackable);
            }
        }

        public static void ApplyTo(
            this IEnumerable<KeyValuePair<string, JObject>> pathAndTrackerObjects,
            ITrackable trackable,
            JsonSerializerSettings? jsonSerializerSettings = null)
        {
            if (pathAndTrackerObjects == null)
                throw new ArgumentNullException(nameof(pathAndTrackerObjects));

            pathAndTrackerObjects
                .Select(x => new KeyValuePair<string, JToken>(x.Key, x.Value))
                .ApplyTo(trackable, jsonSerializerSettings);
        }
    }
}
