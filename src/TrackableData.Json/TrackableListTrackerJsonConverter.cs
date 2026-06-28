using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public sealed class TrackableListTrackerJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TrackableListTracker<T>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonArray = JArray.Load(reader);
            var tracker = new TrackableListTracker<T>();
            foreach (var item in jsonArray)
            {
                if (item is not JArray change || change.Count == 0)
                    throw new JsonSerializationException("List tracker item must be a non-empty JSON array.");

                var operationToken = change[0].ToObject<string>(serializer);
                if (string.IsNullOrEmpty(operationToken))
                    throw new JsonSerializationException("List tracker operation token cannot be empty.");

                var index = ParseIndex(operationToken);
                switch (operationToken![0])
                {
                    case JsonChangeTokens.Add:
                        TrackAdd(tracker, index, change, serializer);
                        break;
                    case JsonChangeTokens.Remove:
                        TrackRemove(tracker, index);
                        break;
                    case JsonChangeTokens.Modify:
                        tracker.TrackModify(index, default!, ReadChangeValue(change, serializer));
                        break;
                    default:
                        throw new JsonSerializationException($"Unknown list tracker operation: {operationToken[0]}.");
                }
            }

            return tracker;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var tracker = (TrackableListTracker<T>)value;
            writer.WriteStartArray();
            foreach (var item in tracker.ChangeList)
            {
                writer.WriteStartArray();
                switch (item.Operation)
                {
                    case TrackableListOperation.Insert:
                        writer.WriteValue(JsonChangeTokens.Add + item.Index.ToString());
                        serializer.Serialize(writer, item.NewValue);
                        break;
                    case TrackableListOperation.Remove:
                        writer.WriteValue(JsonChangeTokens.Remove + item.Index.ToString());
                        break;
                    case TrackableListOperation.Modify:
                        writer.WriteValue(JsonChangeTokens.Modify + item.Index.ToString());
                        serializer.Serialize(writer, item.NewValue);
                        break;
                    case TrackableListOperation.PushFront:
                        writer.WriteValue(JsonChangeTokens.Add + JsonChangeTokens.FrontIndexToken);
                        serializer.Serialize(writer, item.NewValue);
                        break;
                    case TrackableListOperation.PushBack:
                        writer.WriteValue(JsonChangeTokens.Add + JsonChangeTokens.BackIndexToken);
                        serializer.Serialize(writer, item.NewValue);
                        break;
                    case TrackableListOperation.PopFront:
                        writer.WriteValue(JsonChangeTokens.Remove + JsonChangeTokens.FrontIndexToken);
                        break;
                    case TrackableListOperation.PopBack:
                        writer.WriteValue(JsonChangeTokens.Remove + JsonChangeTokens.BackIndexToken);
                        break;
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }

        private static int ParseIndex(string operationToken)
        {
            if (operationToken.Length < 2)
                throw new JsonSerializationException($"Wrong index token: {operationToken}.");

            var indexToken = operationToken.Substring(1);
            if (indexToken == JsonChangeTokens.FrontIndexToken)
                return JsonChangeTokens.FrontIndex;

            if (indexToken == JsonChangeTokens.BackIndexToken)
                return JsonChangeTokens.BackIndex;

            if (!int.TryParse(indexToken, out var index))
                throw new JsonSerializationException($"Invalid list tracker index token: {operationToken}.");

            return index;
        }

        private static void TrackAdd(TrackableListTracker<T> tracker, int index, JArray change, JsonSerializer serializer)
        {
            var value = ReadChangeValue(change, serializer);
            if (index == JsonChangeTokens.FrontIndex)
                tracker.TrackPushFront(value);
            else if (index == JsonChangeTokens.BackIndex)
                tracker.TrackPushBack(value);
            else
                tracker.TrackInsert(index, value);
        }

        private static void TrackRemove(TrackableListTracker<T> tracker, int index)
        {
            if (index == JsonChangeTokens.FrontIndex)
                tracker.TrackPopFront(default!);
            else if (index == JsonChangeTokens.BackIndex)
                tracker.TrackPopBack(default!);
            else
                tracker.TrackRemove(index, default!);
        }

        private static T ReadChangeValue(JArray change, JsonSerializer serializer)
        {
            if (change.Count < 2)
                throw new JsonSerializationException("List tracker change value is missing.");

            return change[1].ToObject<T>(serializer)!;
        }
    }
}
