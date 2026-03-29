using System;
using System.Collections.Generic;
using TrackableData;

// ─────────────────────────────────────────────
// 1. Trackable POCO 정의
//    ITrackablePoco<T> 인터페이스를 구현하면
//    Source Generator가 자동으로 TrackablePlayer 클래스를 생성합니다.
// ─────────────────────────────────────────────

namespace TrackableData.Sample
{
    public interface IPlayer : ITrackablePoco<IPlayer>
    {
        string Name { get; set; }
        int Level { get; set; }
        int Gold { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TrackableData Sample ===\n");

            PocoExample();
            DictionaryExample();
            ListExample();
            SetExample();
        }

        // ─────────────────────────────────────────────
        // 2. POCO 변경 추적
        // ─────────────────────────────────────────────
        static void PocoExample()
        {
            Console.WriteLine("--- POCO Tracking ---");

            // 생성된 TrackablePlayer 인스턴스 생성
            var player = new TrackablePlayer
            {
                Name = "Alice",
                Level = 1,
                Gold = 100
            };

            // Tracker 활성화
            player.SetDefaultTrackerDeep();

            // 프로퍼티 변경 → Tracker가 자동 기록
            player.Level = 5;
            player.Gold = 250;

            // 변경 사항 확인
            var tracker = (TrackablePocoTracker<IPlayer>)player.Tracker;
            Console.WriteLine($"  HasChange: {tracker.HasChange}");
            foreach (var change in tracker.ChangeMap)
            {
                Console.WriteLine($"  {change.Key.Name}: {change.Value.OldValue} -> {change.Value.NewValue}");
            }

            // Tracker 초기화
            tracker.Clear();
            Console.WriteLine($"  After Clear, HasChange: {tracker.HasChange}\n");
        }

        // ─────────────────────────────────────────────
        // 3. Dictionary 변경 추적
        // ─────────────────────────────────────────────
        static void DictionaryExample()
        {
            Console.WriteLine("--- Dictionary Tracking ---");

            var inventory = new TrackableDictionary<string, int>();
            inventory["sword"] = 1;
            inventory["potion"] = 5;

            // Tracker 활성화
            inventory.SetDefaultTrackerDeep();

            // 변경
            inventory.Add("shield", 1);       // Add
            inventory["potion"] = 3;          // Modify
            inventory.Remove("sword");        // Remove

            var tracker = (TrackableDictionaryTracker<string, int>)inventory.Tracker;
            Console.WriteLine($"  HasChange: {tracker.HasChange}");
            foreach (var change in tracker.ChangeMap)
            {
                Console.WriteLine($"  [{change.Key}] {change.Value.Operation}: {change.Value.NewValue}");
            }
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────
        // 4. List 변경 추적
        // ─────────────────────────────────────────────
        static void ListExample()
        {
            Console.WriteLine("--- List Tracking ---");

            var log = new TrackableList<string>();
            log.Add("Event A");
            log.Add("Event B");

            log.SetDefaultTrackerDeep();

            log.Add("Event C");         // PushBack
            log[0] = "Event A (edited)"; // Modify

            var tracker = (TrackableListTracker<string>)log.Tracker;
            Console.WriteLine($"  HasChange: {tracker.HasChange}");
            foreach (var change in tracker.ChangeList)
            {
                Console.WriteLine($"  [{change.Operation}] Index={change.Index}, New={change.NewValue}");
            }
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────
        // 5. Set 변경 추적
        // ─────────────────────────────────────────────
        static void SetExample()
        {
            Console.WriteLine("--- Set Tracking ---");

            var tags = new TrackableSet<string>();
            tags.Add("vip");
            tags.Add("beta-tester");

            tags.SetDefaultTrackerDeep();

            tags.Add("early-access");   // Add
            tags.Remove("beta-tester"); // Remove

            var tracker = (TrackableSetTracker<string>)tags.Tracker;
            Console.WriteLine($"  HasChange: {tracker.HasChange}");
            foreach (var change in tracker.ChangeMap)
            {
                Console.WriteLine($"  [{change.Value}] {change.Key}");
            }
            Console.WriteLine();
        }
    }
}
