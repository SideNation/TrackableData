using System;
using System.Threading;
using MongoDB.Bson;

namespace TrackableData.MongoDB
{
    public static class UniqueInt64Id
    {
        private static ulong _baseId;
        private static int _staticIncrement;

        static UniqueInt64Id()
        {
            // ObjectId's machine/pid fields were removed in MongoDB.Bson 3.x and replaced
            // by a 5-byte process-unique random value (bytes 4..8). Fold it into the
            // machine/process bytes so each process keeps a stable, distinct base id.
            var bytes = ObjectId.GenerateNewId().ToByteArray();
            var machineByte = (byte)(bytes[4] ^ bytes[5] ^ bytes[6]);
            var processByte = (byte)(bytes[7] ^ bytes[8]);
            _baseId = ((ulong)machineByte << 24) | ((ulong)processByte << 16);
        }

        public static void SetMachineNumber(byte number)
        {
            _baseId = (_baseId & 0xFFFFFFFF00FFFFFFUL) | ((ulong)number << 24);
        }

        public static void SetProcessNumber(byte number)
        {
            _baseId = (_baseId & 0xFFFFFFFFFF00FFFFUL) | ((ulong)number << 16);
        }

        // GenerateNewId like MongoDB.ObjectID but use 8 bytes instead of 12 bytes
        // ID = T T T T M P I I (T: Timestamp, M: Machine, P: Process, I: Increment)
        public static long GenerateNewId()
        {
            var increment = (ulong)(Interlocked.Increment(ref _staticIncrement) & 0x0000ffff);
            var timestamp = (ulong)GetUnixTimestamp();
            return (long)(_baseId | increment | (timestamp << 32));
        }

        private static int GetUnixTimestamp()
        {
            var secondsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(secondsSinceEpoch));
            return (int)secondsSinceEpoch;
        }
    }
}
