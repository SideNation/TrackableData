using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace TrackableData.Json.Tests
{
    // Verifies a complex class value (SampleData) survives tracker serialization (the JSON delta
    // format) for every tracker converter, then applies correctly to a target.
    public class ClassValueJsonConverterTest
    {
        private static JsonSerializerSettings CreateSettings()
        {
            return TrackableJsonSerializerSettings.Create();
        }

        [Fact]
        public void DictionaryTracker_ClassValue_RoundTrip_AppliesToTrackable()
        {
            var tracker = new TrackableDictionaryTracker<int, SampleData>();
            tracker.TrackAdd(1, new SampleData { Name = "Alpha", Level = 1 });
            tracker.TrackModify(2, new SampleData { Name = "old", Level = 0 },
                new SampleData { Name = "Beta", Level = 2 });

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableDictionaryTracker<int, SampleData>>(json, settings);

            var target = new TrackableDictionary<int, SampleData>
            {
                { 2, new SampleData { Name = "old", Level = 0 } }
            };
            deserialized.ApplyTo(target);

            Assert.Equal("Alpha", target[1].Name);
            Assert.Equal(1, target[1].Level);
            Assert.Equal("Beta", target[2].Name);
            Assert.Equal(2, target[2].Level);
        }

        [Fact]
        public void ListTracker_ClassValue_RoundTrip_AppliesToTrackable()
        {
            var list = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            list.SetDefaultTrackerDeep();
            list[0] = new SampleData { Name = "AA", Level = 11 };
            list.Add(new SampleData { Name = "C", Level = 3 });

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(list.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableListTracker<SampleData>>(json, settings);

            var target = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            deserialized.ApplyTo(target);

            Assert.Equal(3, target.Count);
            Assert.Equal("AA", target[0].Name);
            Assert.Equal("C", target[2].Name);
            Assert.Equal(3, target[2].Level);
        }

        [Fact]
        public void SetTracker_ClassValue_RoundTrip_AppliesToTrackable()
        {
            var tracker = new TrackableSetTracker<SampleData>();
            tracker.TrackAdd(new SampleData { Name = "X", Level = 1 });
            tracker.TrackAdd(new SampleData { Name = "Y", Level = 2 });

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableSetTracker<SampleData>>(json, settings);

            var target = new TrackableSet<SampleData>();
            deserialized.ApplyTo(target);

            Assert.Equal(2, target.Count);
            Assert.Equal(new[] { "X", "Y" }, target.Select(x => x.Name).OrderBy(n => n));
        }

        [Fact]
        public void PocoTracker_ClassProperty_RoundTrip_AppliesToTrackable()
        {
            var poco = new TrackableJsonPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            poco.SetDefaultTrackerDeep();
            poco.Data = new SampleData { Name = "D2", Level = 50 };

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(poco.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackablePocoTracker<IJsonPocoWithClass>>(json, settings);

            var target = new TrackableJsonPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            deserialized.ApplyTo((IJsonPocoWithClass)target);

            Assert.Equal("D2", target.Data.Name);
            Assert.Equal(50, target.Data.Level);
        }

        [Fact]
        public void ContainerTracker_ClassValues_RoundTrip_AppliesToTrackable()
        {
            var container = new TrackableJsonClassContainer
            {
                Items = new TrackableDictionary<int, SampleData>
                {
                    { 1, new SampleData { Name = "Alpha", Level = 1 } }
                },
                History = new TrackableList<SampleData>
                {
                    new SampleData { Name = "H1", Level = 10 }
                }
            };
            container.Tracker = new TrackableJsonClassContainerTracker();
            container.Items[1] = new SampleData { Name = "AlphaX", Level = 11 };
            container.Items.Add(2, new SampleData { Name = "Beta", Level = 2 });
            container.History.Add(new SampleData { Name = "H2", Level = 20 });

            var settings = CreateSettings();
            var json = JsonConvert.SerializeObject(container.Tracker, settings);
            var deserialized = JsonConvert.DeserializeObject<TrackableJsonClassContainerTracker>(json, settings);

            var target = new TrackableJsonClassContainer
            {
                Items = new TrackableDictionary<int, SampleData>
                {
                    { 1, new SampleData { Name = "Alpha", Level = 1 } }
                },
                History = new TrackableList<SampleData>
                {
                    new SampleData { Name = "H1", Level = 10 }
                }
            };
            deserialized.ApplyTo((IJsonClassContainer)target);

            Assert.Equal("AlphaX", target.Items[1].Name);
            Assert.Equal("Beta", target.Items[2].Name);
            Assert.Equal(2, target.History.Count);
            Assert.Equal("H2", target.History[1].Name);
        }
    }
}
