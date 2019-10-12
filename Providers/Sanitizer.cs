using System;
using System.Text.RegularExpressions;

namespace Emby.Plugins.AnimeKai.Providers
{
    public static class Sanitizer
    {
        public static string SanitizeDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return string.Empty;

            description = description.TakeUpTo("(Source:")
                                     .TakeUpTo("[Written by")
                                     .TakeUpTo("<i>Note:");

            description = description.Replace("—", " - ")
                                     .Replace("<br>", Environment.NewLine)
                                     .Replace("<br/>", Environment.NewLine)
                                     .Replace("<br></br>", Environment.NewLine);

            description = Regex.Replace(description, "[\r\n]{2,}", Environment.NewLine);

            return description.Trim();
        }

        private static string TakeUpTo(this string input, string element)
        {
            var elementIndex = input.IndexOf(element);

            if (elementIndex <= 0) return input;

            return input.Substring(0, elementIndex);
        }
    }
}