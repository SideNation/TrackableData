using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TrackableData.Tests
{
    public class DictionaryTest
    {
        private TrackableDictionary<int, string> CreateTestDictionary()
        {
            return new TrackableDictionary<int, string>()
            {
                { 1, "One" },
                { 2, "Two" },
                { 3, "Three" }
            };
        }

        private TrackableDictionary<int, string> CreateTestDictionaryWithTracker()
        {
            var dict = CreateTestDictionary();
            dict.SetDefaultTrackerDeep();
            return dict;
        }

        [Fact]
        public void TestDictionary_Update_Existing_Key_Updated()
        {
            var dict = CreateTestDictionaryWithTracker();
            var ret = dict.Update(1, (key, value) => value + "!");
            Assert.True(ret);
            Assert.True(dict.Tracker.HasChange);
            Assert.Equal("One!", dict[1]);
        }

        [Fact]
        public void TestDictionary_Update_NonExisting_Key_NotChanged()
        {
            var dict = CreateTestDictionaryWithTracker();
            var ret = dict.Update(-1, (key, value) => value + "!");
            Assert.False(ret);
            Assert.False(dict.Tracker.HasChange);
        }

        [Fact]
        public void TestDictionary_AddOrUpdate_Existing_Key_Updated()
        {
            var dict = CreateTestDictionaryWithTracker();
            var ret = dict.AddOrUpdate(1, (key) => "", (key, value) => value + "!");
            Assert.Equal("One!", ret);
            Assert.True(dict.Tracker.HasChange);
            Assert.Equal("One!", dict[1]);
        }

        [Fact]
        public void TestDictionary_AddOrUpdate_NonExisting_Key_Added()
        {
            var dict = CreateTestDictionaryWithTracker();
            var ret = dict.AddOrUpdate(-1, (key) => "New" + key, (key, value) => value + "!");
            Assert.Equal("New-1", ret);
            Assert.True(dict.Tracker.HasChange);
            Assert.Equal("New-1", dict[-1]);
        }

        [Fact]
        public void TestDictionary_Tracking_Work()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[4] = "FourAdded";

            var changeMap = ((TrackableDictionaryTracker<int, string>)dict.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);

            var change1 = changeMap[1];
            Assert.Equal(TrackableDictionaryOperation.Modify, change1.Operation);
            Assert.Equal("One", change1.OldValue);
            Assert.Equal("OneModified", change1.NewValue);

            var change2 = changeMap[2];
            Assert.Equal(TrackableDictionaryOperation.Remove, change2.Operation);
            Assert.Equal("Two", change2.OldValue);
            Assert.Null(change2.NewValue);

            var change4 = changeMap[4];
            Assert.Equal(TrackableDictionaryOperation.Add, change4.Operation);
            Assert.Null(change4.OldValue);
            Assert.Equal("FourAdded", change4.NewValue);
        }

        [Fact]
        public void TestDictionary_HasChangedSetEvent_Work()
        {
            var changed = false;
            var dict = CreateTestDictionaryWithTracker();
            dict.Tracker.HasChangeSet += _ => { changed = true; };
            dict[1] = "OneModified";
            Assert.True(changed);
        }

        [Fact]
        public void TestDictionary_ApplyToTrackable_Work()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[4] = "FourAdded";

            var dict2 = CreateTestDictionary();
            dict.Tracker.ApplyTo(dict2);

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<int, string>(1, "OneModified"),
                    new KeyValuePair<int, string>(3, "Three"),
                    new KeyValuePair<int, string>(4, "FourAdded")
                },
                dict2.OrderBy(kv => kv.Key));
        }

        [Fact]
        public void TestDictionary_ApplyToTracker_Work()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[4] = "FourAdded";

            var tracker2 = new TrackableDictionaryTracker<int, string>();
            dict.Tracker.ApplyTo(tracker2);

            var dict2 = CreateTestDictionary();
            tracker2.ApplyTo(dict2);

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<int, string>(1, "OneModified"),
                    new KeyValuePair<int, string>(3, "Three"),
                    new KeyValuePair<int, string>(4, "FourAdded")
                },
                dict2.OrderBy(kv => kv.Key));
        }

        [Fact]
        public void TestDictionary_RollbackToTrackable_Work()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[4] = "FourAdded";

            var dict2 = CreateTestDictionary();
            dict.Tracker.ApplyTo(dict2);
            dict.Tracker.RollbackTo(dict2);

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<int, string>(1, "One"),
                    new KeyValuePair<int, string>(2, "Two"),
                    new KeyValuePair<int, string>(3, "Three")
                },
                dict2.OrderBy(kv => kv.Key));
        }

        [Fact]
        public void TestDictionary_RollbackToTracker_Work()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[4] = "FourAdded";

            var tracker2 = new TrackableDictionaryTracker<int, string>();
            dict.Tracker.ApplyTo(tracker2);
            dict.Tracker.RollbackTo(tracker2);

            var dict2 = CreateTestDictionary();
            tracker2.ApplyTo(dict2);

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<int, string>(1, "One"),
                    new KeyValuePair<int, string>(2, "Two"),
                    new KeyValuePair<int, string>(3, "Three")
                },
                dict2.OrderBy(kv => kv.Key));
        }

        [Fact]
        public void TestDictionary_Clone_Work()
        {
            var a = CreateTestDictionaryWithTracker();
            var b = a.Clone();

            Assert.Null(b.Tracker);
            Assert.False(ReferenceEquals(a._dictionary, b._dictionary));
            Assert.Equal(a._dictionary, b._dictionary);
        }

        [Fact]
        public void TestDictionary_Clear_TracksAllRemoves()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict.Clear();

            var changeMap = ((TrackableDictionaryTracker<int, string>)dict.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);
            Assert.All(changeMap.Values, c => Assert.Equal(TrackableDictionaryOperation.Remove, c.Operation));
        }

        [Fact]
        public void TestDictionary_NoTracker_NoException()
        {
            var dict = CreateTestDictionary();
            dict[1] = "Modified";
            dict.Remove(2);
            dict[4] = "Added";
            Assert.False(dict.Changed);
        }

        // --- Tracker operation sequence edge cases ---

        [Fact]
        public void TestDictionaryTracker_AddThenRemove_CancelsOut()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackRemove(10, "ten");
            Assert.False(tracker.HasChange);
            Assert.Empty(tracker.ChangeMap);
        }

        [Fact]
        public void TestDictionaryTracker_RemoveThenAdd_BecomesModify()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackRemove(1, "Old");
            tracker.TrackAdd(1, "New");

            Assert.Single(tracker.ChangeMap);
            var change = tracker.ChangeMap[1];
            Assert.Equal(TrackableDictionaryOperation.Modify, change.Operation);
            Assert.Equal("Old", change.OldValue);
            Assert.Equal("New", change.NewValue);
        }

        [Fact]
        public void TestDictionaryTracker_ModifyThenRemove_BecomesRemoveWithOriginalOld()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackModify(1, "Original", "Modified");
            tracker.TrackRemove(1, "Modified");

            Assert.Single(tracker.ChangeMap);
            var change = tracker.ChangeMap[1];
            Assert.Equal(TrackableDictionaryOperation.Remove, change.Operation);
            Assert.Equal("Original", change.OldValue);
        }

        [Fact]
        public void TestDictionaryTracker_AddThenModify_StaysAddWithNewValue()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "First");
            tracker.TrackModify(1, "First", "Second");

            Assert.Single(tracker.ChangeMap);
            var change = tracker.ChangeMap[1];
            Assert.Equal(TrackableDictionaryOperation.Add, change.Operation);
            Assert.Equal("Second", change.NewValue);
        }

        [Fact]
        public void TestDictionaryTracker_ModifyThenModify_KeepsOriginalOld()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackModify(1, "Original", "First");
            tracker.TrackModify(1, "First", "Second");

            Assert.Single(tracker.ChangeMap);
            var change = tracker.ChangeMap[1];
            Assert.Equal(TrackableDictionaryOperation.Modify, change.Operation);
            Assert.Equal("Original", change.OldValue);
            Assert.Equal("Second", change.NewValue);
        }

        // --- Exception scenarios ---

        [Fact]
        public void TestDictionaryTracker_AddAfterAdd_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "one");
            Assert.Throws<InvalidOperationException>(() => tracker.TrackAdd(1, "one again"));
        }

        [Fact]
        public void TestDictionaryTracker_RemoveAfterRemove_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackRemove(1, "one");
            Assert.Throws<InvalidOperationException>(() => tracker.TrackRemove(1, "one again"));
        }

        [Fact]
        public void TestDictionaryTracker_ModifyAfterRemove_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackRemove(1, "one");
            Assert.Throws<InvalidOperationException>(() => tracker.TrackModify(1, "one", "two"));
        }

        // --- Enumerable helpers ---

        [Fact]
        public void TestDictionaryTracker_AddItems_ReturnsOnlyAdds()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackRemove(1, "one");
            tracker.TrackModify(2, "two", "TWO");

            var adds = new List<KeyValuePair<int, string>>(tracker.AddItems);
            Assert.Single(adds);
            Assert.Equal(10, adds[0].Key);
            Assert.Equal("ten", adds[0].Value);
        }

        [Fact]
        public void TestDictionaryTracker_ModifyItems_ReturnsOnlyModifies()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackRemove(1, "one");
            tracker.TrackModify(2, "two", "TWO");

            var mods = new List<KeyValuePair<int, string>>(tracker.ModifyItems);
            Assert.Single(mods);
            Assert.Equal(2, mods[0].Key);
            Assert.Equal("TWO", mods[0].Value);
        }

        [Fact]
        public void TestDictionaryTracker_RemoveItems_ReturnsOnlyRemoves()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackRemove(1, "one");
            tracker.TrackModify(2, "two", "TWO");

            var removes = new List<KeyValuePair<int, string>>(tracker.RemoveItems);
            Assert.Single(removes);
            Assert.Equal(1, removes[0].Key);
            Assert.Equal("one", removes[0].Value);
        }

        [Fact]
        public void TestDictionaryTracker_RemoveKeys_ReturnsOnlyRemoveKeys()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackRemove(1, "one");
            tracker.TrackRemove(5, "five");

            var keys = new List<int>(tracker.RemoveKeys);
            Assert.Equal(2, keys.Count);
            Assert.Contains(1, keys);
            Assert.Contains(5, keys);
        }

        // --- ApplyTo strict mode ---

        [Fact]
        public void TestDictionaryTracker_ApplyToStrict_ThrowsOnDuplicateAdd()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "one");

            var dict = new Dictionary<int, string> { { 1, "existing" } };
            Assert.Throws<ArgumentException>(() => tracker.ApplyTo(dict, true));
        }

        [Fact]
        public void TestDictionaryTracker_ApplyToNonStrict_OverwritesOnAdd()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "one");

            var dict = new Dictionary<int, string> { { 1, "existing" } };
            tracker.ApplyTo(dict, false);
            Assert.Equal("one", dict[1]);
        }

        // --- Null guard ---

        [Fact]
        public void TestDictionaryTracker_ApplyTo_NullTrackable_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((IDictionary<int, string>)null!));
        }

        [Fact]
        public void TestDictionaryTracker_ApplyTo_NullTracker_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((TrackableDictionaryTracker<int, string>)null!));
        }

        [Fact]
        public void TestDictionaryTracker_RollbackTo_NullTrackable_Throws()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            Assert.Throws<ArgumentNullException>(() => tracker.RollbackTo((IDictionary<int, string>)null!));
        }

        // --- ToString ---

        [Fact]
        public void TestDictionaryTracker_ToString_ShowsAllOperations()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "one");
            tracker.TrackRemove(2, "two");
            tracker.TrackModify(3, "three", "THREE");

            var str = tracker.ToString();
            Assert.Contains("+1", str);
            Assert.Contains("-2", str);
            Assert.Contains("=3", str);
        }

        // --- HasChangeSet event ---

        [Fact]
        public void TestDictionaryTracker_HasChangeSet_NotFiredOnSubsequentChanges()
        {
            var callCount = 0;
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackAdd(1, "one");
            tracker.TrackAdd(2, "two");
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void TestDictionaryTracker_HasChangeSet_FiredAgainAfterClearAndNewChange()
        {
            var callCount = 0;
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackAdd(1, "one");
            tracker.Clear();
            tracker.TrackAdd(2, "two");
            Assert.Equal(2, callCount);
        }

        // --- Indexer edge cases ---

        [Fact]
        public void TestDictionary_Indexer_SetExistingKey_TracksModify()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[1] = "Modified";

            var changeMap = ((TrackableDictionaryTracker<int, string>)dict.Tracker).ChangeMap;
            Assert.Equal(TrackableDictionaryOperation.Modify, changeMap[1].Operation);
        }

        [Fact]
        public void TestDictionary_Indexer_SetNewKey_TracksAdd()
        {
            var dict = CreateTestDictionaryWithTracker();
            dict[99] = "New";

            var changeMap = ((TrackableDictionaryTracker<int, string>)dict.Tracker).ChangeMap;
            Assert.Equal(TrackableDictionaryOperation.Add, changeMap[99].Operation);
        }

        // --- Constructor variants ---

        [Fact]
        public void TestDictionary_CopyConstructor_CopiesData()
        {
            var original = CreateTestDictionary();
            var copy = new TrackableDictionary<int, string>(original);

            Assert.Equal(3, copy.Count);
            Assert.Equal("One", copy[1]);
            Assert.Null(copy.Tracker);
        }

        [Fact]
        public void TestDictionary_DictionaryConstructor_CopiesData()
        {
            var source = new Dictionary<int, string> { { 1, "A" }, { 2, "B" } };
            var dict = new TrackableDictionary<int, string>(source);

            Assert.Equal(2, dict.Count);
            Assert.Equal("A", dict[1]);
        }

        // --- ICollection operations ---

        [Fact]
        public void TestDictionary_RemoveKeyValuePair_TracksRemove()
        {
            var dict = CreateTestDictionaryWithTracker();
            var removed = ((ICollection<KeyValuePair<int, string>>)dict).Remove(
                new KeyValuePair<int, string>(1, "One"));

            Assert.True(removed);
            var changeMap = ((TrackableDictionaryTracker<int, string>)dict.Tracker).ChangeMap;
            Assert.Equal(TrackableDictionaryOperation.Remove, changeMap[1].Operation);
        }

        [Fact]
        public void TestDictionary_RemoveKeyValuePair_WrongValue_ReturnsFalse()
        {
            var dict = CreateTestDictionaryWithTracker();
            var removed = ((ICollection<KeyValuePair<int, string>>)dict).Remove(
                new KeyValuePair<int, string>(1, "Wrong"));

            Assert.False(removed);
            Assert.False(dict.Tracker.HasChange);
        }
    }
}
