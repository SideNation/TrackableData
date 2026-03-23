using System;
using System.Linq;
using Xunit;

namespace TrackableData.Generator.Tests
{
    public class PocoGeneratorTest
    {
        [Fact]
        public void TestGenerated_TrackablePerson_Exists()
        {
            var person = new TrackablePerson();
            Assert.NotNull(person);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_PropertySetGet()
        {
            var person = new TrackablePerson
            {
                Name = "Alice",
                Age = 30,
                Id = 12345
            };

            Assert.Equal("Alice", person.Name);
            Assert.Equal(30, person.Age);
            Assert.Equal((UInt128)12345, person.Id);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_Tracking()
        {
            var person = new TrackablePerson();
            person.SetDefaultTrackerDeep();
            person.Name = "Alice";
            person.Age = 30;

            Assert.True(person.Changed);
            var tracker = (TrackablePocoTracker<IPerson>)person.Tracker;
            Assert.Equal(2, tracker.ChangeMap.Count);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_NoChangeWhenSameValue()
        {
            var person = new TrackablePerson();
            person.Name = "Alice";
            person.SetDefaultTrackerDeep();

            // Set same value
            person.Name = "Alice";
            Assert.False(person.Changed);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_ApplyTo()
        {
            var person = new TrackablePerson();
            person.Name = "Alice";
            person.Age = 25;
            person.SetDefaultTrackerDeep();

            person.Name = "Bob";
            person.Age = 30;

            var person2 = new TrackablePerson();
            person2.Name = "Alice";
            person2.Age = 25;
            person.Tracker.ApplyTo(person2);

            Assert.Equal("Bob", person2.Name);
            Assert.Equal(30, person2.Age);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_RollbackTo()
        {
            var person = new TrackablePerson();
            person.Name = "Alice";
            person.Age = 25;
            person.SetDefaultTrackerDeep();

            person.Name = "Bob";
            person.Age = 30;

            person.Tracker.RollbackTo(person);

            Assert.Equal("Alice", person.Name);
            Assert.Equal(25, person.Age);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_Clone()
        {
            var person = new TrackablePerson();
            person.Name = "Alice";
            person.Age = 25;
            person.SetDefaultTrackerDeep();

            var clone = person.Clone();

            Assert.Equal("Alice", clone.Name);
            Assert.Equal(25, clone.Age);
            Assert.Null(clone.Tracker);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_PropertyTable()
        {
            Assert.NotNull(TrackablePerson.PropertyTable.Name);
            Assert.NotNull(TrackablePerson.PropertyTable.Age);
            Assert.Equal("Name", TrackablePerson.PropertyTable.Name.Name);
            Assert.Equal("Age", TrackablePerson.PropertyTable.Age.Name);
        }

        [Fact]
        public void TestGenerated_TrackablePerson_ImplementsInterface()
        {
            var person = new TrackablePerson();
            Assert.IsAssignableFrom<IPerson>(person);
            Assert.IsAssignableFrom<ITrackable>(person);
            Assert.IsAssignableFrom<ITrackablePoco<IPerson>>(person);
        }
    }
}
