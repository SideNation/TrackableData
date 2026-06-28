using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace TrackableData.Json.Tests
{
    // Verifies the AOT / Unity IL2CPP-safe path: explicit converter registration with no
    // auto-dispatching TrackerJsonConverter (so no runtime MakeGenericType).
    public class AotJsonConverterTest
    {
        [Fact]
        public void CreateForAot_DoesNotRegisterReflectionDispatcher()
        {
            var settings = TrackableJsonSerializerSettings.CreateForAot();
            Assert.DoesNotContain(settings.Converters, c => c is TrackerJsonConverter);
        }

        [Fact]
        public void PocoTracker_AotRegistration_RoundTrip()
        {
            var settings = TrackableJsonSerializerSettings.CreateForAot()
                .AddPocoTrackerConverter<IJsonPerson>();

            var person = new TrackableJsonPerson { Name = "Alice", Age = 20 };
            person.SetDefaultTrackerDeep();
            person.Name = "Bob";
            person.Age = 30;

            var json = JsonConvert.SerializeObject(person.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackablePocoTracker<IJsonPerson>>(json, settings);

            var target = new TrackableJsonPerson { Name = "Alice", Age = 20 };
            deserialized.ApplyTo((IJsonPerson)target);

            Assert.Equal("Bob", target.Name);
            Assert.Equal(30, target.Age);
        }

        [Fact]
        public void DictionaryTracker_AotRegistration_RoundTrip()
        {
            var settings = TrackableJsonSerializerSettings.CreateForAot()
                .AddDictionaryTrackerConverter<int, string>();

            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "One");
            tracker.TrackModify(2, "old", "Two");

            var json = JsonConvert.SerializeObject(tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableDictionaryTracker<int, string>>(json, settings);

            var target = new TrackableDictionary<int, string> { { 2, "old" } };
            deserialized.ApplyTo(target);

            Assert.Equal("One", target[1]);
            Assert.Equal("Two", target[2]);
        }

        [Fact]
        public void ContainerTracker_AotRegistration_RoundTrip()
        {
            var settings = TrackableJsonSerializerSettings.CreateForAot()
                .AddContainerTrackerConverter()
                .AddDictionaryTrackerConverter<int, string>()
                .AddListTrackerConverter<string>()
                .AddSetTrackerConverter<int>();

            var container = new TrackableJsonDataContainer
            {
                Names = new TrackableDictionary<int, string> { { 1, "One" } },
                Events = new TrackableList<string> { "Start" },
                Values = new TrackableSet<int> { 1 }
            };
            container.Tracker = new TrackableJsonDataContainerTracker();
            container.Names[1] = "OneModified";
            container.Events.Add("End");
            container.Values.Add(2);

            var json = JsonConvert.SerializeObject(container.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableJsonDataContainerTracker>(json, settings);

            var target = new TrackableJsonDataContainer
            {
                Names = new TrackableDictionary<int, string> { { 1, "One" } },
                Events = new TrackableList<string> { "Start" },
                Values = new TrackableSet<int> { 1 }
            };
            deserialized.ApplyTo((IJsonDataContainer)target);

            Assert.Equal("OneModified", target.Names[1]);
            Assert.Equal(new[] { "Start", "End" }, target.Events);
            Assert.Equal(new[] { 1, 2 }, target.Values.OrderBy(x => x));
        }
    }
}
