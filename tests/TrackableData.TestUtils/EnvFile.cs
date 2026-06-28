using System;
using System.IO;

namespace TrackableData.TestUtils
{
    public static class EnvFile
    {
        private const string EnvFileName = ".env";

        public static string GetValue(string name)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var envFilePath = Path.Combine(directory.FullName, EnvFileName);
                if (File.Exists(envFilePath))
                    return ReadValue(envFilePath, name);

                directory = directory.Parent;
            }
            return null;
        }

        private static string ReadValue(string envFilePath, string name)
        {
            foreach (var line in File.ReadLines(envFilePath))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length == 0 || trimmedLine.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex < 0)
                    continue;

                var key = trimmedLine.Substring(0, separatorIndex).Trim();
                if (!string.Equals(key, name, StringComparison.Ordinal))
                    continue;

                var value = trimmedLine.Substring(separatorIndex + 1).Trim();
                return Unquote(value);
            }
            return null;
        }

        private static string Unquote(string value)
        {
            if (value.Length < 2)
                return value;

            if ((value[0] == '"' && value[value.Length - 1] == '"') ||
                (value[0] == '\'' && value[value.Length - 1] == '\''))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}
