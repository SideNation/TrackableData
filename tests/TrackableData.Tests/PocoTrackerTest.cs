using System;
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

        // --- ApplyTo object ---

        [Fact]
        public void TestPocoTracker_ApplyTo_Object_SetsProperties()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");
            tracker.TrackSet(AgeProperty, 10, 20);

            var poco = new SamplePoco { Name = "Old", Age = 10 };
            tracker.ApplyTo(poco);

            Assert.Equal("New", poco.Name);
            Assert.Equal(20, poco.Age);
        }

        [Fact]
        public void TestPocoTracker_ApplyTo_TypedTrackable_SetsProperties()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");

            ISamplePoco poco = new SamplePoco { Name = "Old", Age = 10 };
            tracker.ApplyTo(poco);

            Assert.Equal("New", poco.Name);
            Assert.Equal(10, poco.Age); // unchanged
        }

        // --- RollbackTo object ---

        [Fact]
        public void TestPocoTracker_RollbackTo_Object_RestoresOldValues()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");
            tracker.TrackSet(AgeProperty, 10, 20);

            var poco = new SamplePoco { Name = "New", Age = 20 };
            tracker.RollbackTo(poco);

            Assert.Equal("Old", poco.Name);
            Assert.Equal(10, poco.Age);
        }

        [Fact]
        public void TestPocoTracker_RollbackTo_TypedTrackable_RestoresOldValues()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Original", "Modified");

            ISamplePoco poco = new SamplePoco { Name = "Modified", Age = 25 };
            tracker.RollbackTo(poco);

            Assert.Equal("Original", poco.Name);
            Assert.Equal(25, poco.Age); // unchanged
        }

        // --- Null guard ---

        [Fact]
        public void TestPocoTracker_ApplyTo_NullTrackable_Throws()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((ISamplePoco)null!));
        }

        [Fact]
        public void TestPocoTracker_ApplyTo_NullTracker_Throws()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((TrackablePocoTracker<ISamplePoco>)null!));
        }

        [Fact]
        public void TestPocoTracker_RollbackTo_NullTrackable_Throws()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            Assert.Throws<ArgumentNullException>(() => tracker.RollbackTo((ISamplePoco)null!));
        }

        [Fact]
        public void TestPocoTracker_RollbackTo_NullTracker_Throws()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            Assert.Throws<ArgumentNullException>(() => tracker.RollbackTo((TrackablePocoTracker<ISamplePoco>)null!));
        }

        // --- HasChangeSet edge cases ---

        [Fact]
        public void TestPocoTracker_HasChangeSet_FiredAgainAfterClearAndNewChange()
        {
            var callCount = 0;
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackSet(NameProperty, "a", "b");
            tracker.Clear();
            tracker.TrackSet(NameProperty, "c", "d");
            Assert.Equal(2, callCount);
        }

        // --- No change after clear ---

        [Fact]
        public void TestPocoTracker_NoChangeAfterClear()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", "New");
            Assert.True(tracker.HasChange);
            tracker.Clear();
            Assert.False(tracker.HasChange);
        }

        // --- TrackSet with null values ---

        [Fact]
        public void TestPocoTracker_TrackSet_NullOldValue()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, null, "New");

            var change = tracker.ChangeMap[NameProperty];
            Assert.Null(change.OldValue);
            Assert.Equal("New", change.NewValue);
        }

        [Fact]
        public void TestPocoTracker_TrackSet_NullNewValue()
        {
            var tracker = new TrackablePocoTracker<ISamplePoco>();
            tracker.TrackSet(NameProperty, "Old", null);

            var change = tracker.ChangeMap[NameProperty];
            Assert.Equal("Old", change.OldValue);
            Assert.Null(change.NewValue);
        }

        // --- ApplyTo tracker then apply to object ---

        [Fact]
        public void TestPocoTracker_ChainedApply_Work()
        {
            var tracker1 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.TrackSet(NameProperty, "Original", "Modified");
            tracker1.TrackSet(AgeProperty, 10, 30);

            var tracker2 = new TrackablePocoTracker<ISamplePoco>();
            tracker1.ApplyTo(tracker2);

            var poco = new SamplePoco { Name = "Original", Age = 10 };
            tracker2.ApplyTo(poco);

            Assert.Equal("Modified", poco.Name);
            Assert.Equal(30, poco.Age);
        }
    }

    public interface ISamplePoco
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    public class SamplePoco : ISamplePoco
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
