namespace TrackableData.Json
{
    internal static class JsonChangeTokens
    {
        public const char Add = '+';
        public const char Remove = '-';
        public const char Modify = '=';

        public const string AddPropertyName = "+";
        public const string RemovePropertyName = "-";
        public const string FrontIndexToken = "F";
        public const string BackIndexToken = "B";
        public const string TrackerSuffix = "Tracker";

        public const int FrontIndex = -2;
        public const int BackIndex = -1;
    }
}
