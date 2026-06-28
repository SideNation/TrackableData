using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace TrackableData.Json.Tests
{
    public class TrackerJsonConverterTest
    {
        private static JsonSerializerSettings CreateSettings()
        {
            return TrackableJsonSerializerSettings.Create();
        }

        [Fact]
        public void TestPocoTracker_RoundTrip_AppliesToTrackable()
        {
            var person = new TrackableJsonPerson
            {
                Name = "Alice",
                Age = 20
            };
            person.SetDefaultTrackerDeep();
            person.Name = "Bob";
            person.Age = 30;

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(person.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackablePocoTracker<IJsonPerson>>(json, settings);

            var target = new TrackableJsonPerson
            {
                Name = "Alice",
                Age = 20
            };
            deserialized.ApplyTo((IJsonPerson)target);

            Assert.Equal("Bob", target.Name);
            Assert.Equal(30, target.Age);
        }

        [Fact]
        public void TestDictionaryTracker_RoundTrip_AppliesToTrackable()
        {
            var tracker = new TrackableDictionaryTracker<string, int>();
            tracker.TrackAdd("shield", 1);
            tracker.TrackModify("potion", 5, 3);
            tracker.TrackRemove("sword", 1);

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableDictionaryTracker<string, int>>(json, settings);

            var target = new TrackableDictionary<string, int>
            {
                { "sword", 1 },
                { "potion", 5 }
            };
            deserialized.ApplyTo(target);

            Assert.False(target.ContainsKey("sword"));
            Assert.Equal(3, target["potion"]);
            Assert.Equal(1, target["shield"]);
        }

        [Fact]
        public void TestListTracker_RoundTrip_AppliesToTrackable()
        {
            var list = new TrackableList<string> { "A", "B" };
            list.SetDefaultTrackerDeep();
            list[0] = "AA";
            list.Add("C");
            list.RemoveAt(1);

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(list.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableListTracker<string>>(json, settings);

            var target = new TrackableList<string> { "A", "B" };
            deserialized.ApplyTo(target);

            Assert.Equal(new[] { "AA", "C" }, target);
        }

        [Fact]
        public void TestSetTracker_RoundTrip_AppliesToTrackable()
        {
            var set = new TrackableSet<int> { 1, 2 };
            set.SetDefaultTrackerDeep();
            set.Remove(1);
            set.Add(3);

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(set.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableSetTracker<int>>(json, settings);

            var target = new TrackableSet<int> { 1, 2 };
            deserialized.ApplyTo(target);

            Assert.Equal(new[] { 2, 3 }, target.OrderBy(x => x));
        }

        [Fact]
        public void TestContainerTracker_RoundTrip_AppliesToTrackable()
        {
            var container = CreateContainer();
            container.Tracker = new TrackableJsonDataContainerTracker();
            container.Names[1] = "OneModified";
            container.Events.Add("End");
            container.Values.Remove(1);
            container.Values.Add(2);

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(container.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableJsonDataContainerTracker>(json, settings);

            var target = CreateContainer();
            deserialized.ApplyTo((IJsonDataContainer)target);

            Assert.Equal("OneModified", target.Names[1]);
            Assert.Equal(new[] { "Start", "End" }, target.Events);
            Assert.Equal(new[] { 2 }, target.Values.OrderBy(x => x));
        }

        [Fact]
        public void TestSerializeChangedTrackersWithPath_RoundTrip_AppliesToTrackable()
        {
            var dictionary = new TrackableDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };
            dictionary.SetDefaultTrackerDeep();
            dictionary[1] = "OneModified";
            dictionary.Remove(2);
            dictionary[3] = "Three";

            var json = dictionary.SerializeChangedTrackersWithPath();

            var target = new TrackableDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };
            json.ApplyTo(target);

            Assert.Equal("OneModified", target[1]);
            Assert.False(target.ContainsKey(2));
            Assert.Equal("Three", target[3]);
        }

        [Fact]
        public void TestSerializeChangedTrackersWithPath_ListRoundTrip_AppliesToTrackable()
        {
            var list = new TrackableList<string> { "A", "B" };
            list.SetDefaultTrackerDeep();
            list.Add("C");
            list[0] = "AA";

            var json = list.SerializeChangedTrackersWithPath();

            var target = new TrackableList<string> { "A", "B" };
            json.ApplyTo(target);

            Assert.Equal(new[] { "AA", "B", "C" }, target);
        }

        private static TrackableJsonDataContainer CreateContainer()
        {
            return new TrackableJsonDataContainer
            {
                Names = new TrackableDictionary<int, string>
                {
                    { 1, "One" }
                },
                Events = new TrackableList<string>
                {
                    "Start"
                },
                Values = new TrackableSet<int>
                {
                    1
                }
            };
        }
    }
}
