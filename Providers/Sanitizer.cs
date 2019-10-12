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

            description = description.Replace("—", " - ");

            description = description.Trim().TrimBreaksFromEnd();

            return description;
        }

        private static string TrimBreaksFromEnd(this string input)
        {
            while (true)
            {
                if (input.EndsWith("<br>"))
                    input = input.RemoveFromEnd("<br>");
                else if (input.EndsWith("<br/>"))
                    input = input.RemoveFromEnd("<br/>");
                else if (input.EndsWith("<br></br>"))
                    input = input.RemoveFromEnd("<br></br>");
                else
                    break;
            }

            return input;
        }

        private static string RemoveFromEnd(this string input, string element)
        {
            return input.Remove(input.Length - element.Length).TrimEnd();
        }

        private static string TakeUpTo(this string input, string element)
        {
            var sourceIndex = input.IndexOf(element);

            if (sourceIndex <= 0) return input;

            return input.Substring(0, sourceIndex);
        }
    }
}