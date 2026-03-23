using System.Linq;
using System.Reflection;
using Xunit;

namespace TrackableData.Tests
{
    public class PocoTrackerTest
    {
        private static readonly PropertyInfo NameProperty = typeof(ISamplePoco).GetProperty("Name")!;
        private static readonly PropertyInfo AgeProperty = typeof(ISamplePoco).GetProperty("Age")!;

        [Fact]
        public void TestPocoTracker_TrackSet_RecordsChange()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");

            Assert.True(tracker.HasChange);
            Assert.Single(tracker.ChangeMap);

            var change = tracker.ChangeMap[NameProperty];
            Assert.Equal("Old", change.OldValue);
            Assert.Equal("New", change.NewValue);
        }

        [Fact]
        public void TestPocoTracker_OverlappedTracking_KeepsOriginalOldValue()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Original", "First");
            tracker.TrackSet(NameProperty, "First", "Second");

            Assert.Single(tracker.ChangeMap);

            var change = tracker.ChangeMap[NameProperty];
            Assert.Equal("Original", change.OldValue);
            Assert.Equal("Second", change.NewValue);
        }

        [Fact]
        public void TestPocoTracker_HasChangedSetEvent_Work()
        {
            var changed = false;
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.HasChangeSet += _ => { changed = true; };
            tracker.TrackSet(NameProperty, "Old", "New");
            Assert.True(changed);
        }

        [Fact]
        public void TestPocoTracker_HasChangedSetEvent_NotFiredOnSubsequentChanges()
        {
            var callCount = 0;
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackSet(NameProperty, "Old", "New");
            tracker.TrackSet(AgeProperty, 10, 20);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void TestPocoTracker_Clear_RemovesAllChanges()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");
            tracker.TrackSet(AgeProperty, 10, 20);

            tracker.Clear();

            Assert.False(tracker.HasChange);
            Assert.Empty(tracker.ChangeMap);
        }

        [Fact]
        public void TestPocoTracker_ApplyToTracker_Work()
        {
            var tracker1 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.TrackSet(NameProperty, "Old", "New");
            tracker1.TrackSet(AgeProperty, 10, 20);

            var tracker2 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.ApplyTo(tracker2);

            Assert.Equal(2, tracker2.ChangeMap.Count);
            Assert.Equal("Old", tracker2.ChangeMap[NameProperty].OldValue);
            Assert.Equal("New", tracker2.ChangeMap[NameProperty].NewValue);
        }

        [Fact]
        public void TestPocoTracker_RollbackToSelf_Clears()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");

            tracker.RollbackTo(tracker);

            Assert.False(tracker.HasChange);
        }

        [Fact]
        public void TestPocoTracker_RollbackToTracker_SwapsValues()
        {
            var tracker1 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.TrackSet(NameProperty, "Old", "New");

            var tracker2 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.RollbackTo(tracker2);

            Assert.Single(tracker2.ChangeMap);
            Assert.Equal("New", tracker2.ChangeMap[NameProperty].OldValue);
            Assert.Equal("Old", tracker2.ChangeMap[NameProperty].NewValue);
        }

        [Fact]
        public void TestPocoTracker_MultipleProperties_Work()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Alice", "Bob");
            tracker.TrackSet(AgeProperty, 25, 30);

            Assert.Equal(2, tracker.ChangeMap.Count);
            Assert.Equal("Alice", tracker.ChangeMap[NameProperty].OldValue);
            Assert.Equal("Bob", tracker.ChangeMap[NameProperty].NewValue);
            Assert.Equal(25, tracker.ChangeMap[AgeProperty].OldValue);
            Assert.Equal(30, tracker.ChangeMap[AgeProperty].NewValue);
        }

        [Fact]
        public void TestPocoTracker_ToString_Work()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");

            var str = tracker.ToString();
            Assert.Contains("Name", str);
            Assert.Contains("Old", str);
            Assert.Contains("New", str);
        }
    }

    public interface ISamplePoco
    {
        string Name { get; set; }
        int Age { get; set; }
    }
}
