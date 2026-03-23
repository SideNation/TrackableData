using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TrackableData.Generator.Tests
{
    public class ContainerGeneratorTest
    {
        [Fact]
        public void TestGenerated_TrackableDataContainer_Exists()
        {
            var container = new TrackableDataContainer();
            Assert.NotNull(container);
            Assert.NotNull(container.Names);
            Assert.NotNull(container.Tags);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_Tracking()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            Assert.True(container.Changed);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_Clear()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            container.Tracker.Clear();
            Assert.False(container.Changed);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_ApplyTo()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            var container2 = new TrackableDataContainer();
            container.Tracker.ApplyTo((IDataContainer)container2);

            Assert.Equal("Alice", container2.Names[1]);
            Assert.Contains("tag1", container2.Tags);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_RollbackTo()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            container.Tracker.RollbackTo((IDataContainer)container);

            Assert.Empty(container.Names);
            Assert.Empty(container.Tags);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_Clone()
        {
            var container = new TrackableDataContainer();
            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            var clone = container.Clone();

            Assert.Equal("Alice", clone.Names[1]);
            Assert.Contains("tag1", clone.Tags);
            Assert.Null(clone.Tracker);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_GetChildTrackables()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";

            var changed = container.GetChildTrackables(true).ToList();
            Assert.Single(changed);
            Assert.Equal("Names", changed[0].Key);
        }

        [Fact]
        public void TestGenerated_TrackableDataContainer_ImplementsInterface()
        {
            var container = new TrackableDataContainer();
            Assert.IsAssignableFrom<IDataContainer>(container);
            Assert.IsAssignableFrom<ITrackable>(container);
            Assert.IsAssignableFrom<ITrackableContainer<IDataContainer>>(container);
        }

        [Fact]
        public void TestGenerated_Tracker_ApplyToTracker()
        {
            var container = new TrackableDataContainer();
            container.Tracker = new TrackableDataContainerTracker();

            container.Names[1] = "Alice";
            container.Tags.Add("tag1");

            var tracker2 = new TrackableDataContainerTracker();
            container.Tracker.ApplyTo(tracker2);

            var container2 = new TrackableDataContainer();
            tracker2.ApplyTo((IDataContainer)container2);

            Assert.Equal("Alice", container2.Names[1]);
            Assert.Contains("tag1", container2.Tags);
        }
    }
}
