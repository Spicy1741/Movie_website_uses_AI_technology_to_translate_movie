using Film_website.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Film_website.Services
{
    public class SrtGeneratorService : ISrtGeneratorService
    {
        private readonly Regex _timeRegex = new(@"(\d{2}):(\d{2}):(\d{2}),(\d{3}) --> (\d{2}):(\d{2}):(\d{2}),(\d{3})");

        public string GenerateSrt(List<SrtEntry> entries)
        {
            var srt = new StringBuilder();

            foreach (var entry in entries)
            {
                srt.AppendLine(entry.Index.ToString());
                srt.AppendLine($"{FormatTime(entry.StartTime)} --> {FormatTime(entry.EndTime)}");
                srt.AppendLine(string.IsNullOrEmpty(entry.TranslatedText) ? entry.Text : entry.TranslatedText);
                srt.AppendLine();
            }

            return srt.ToString();
        }

        public List<SrtEntry> ParseSrt(string srtContent)
        {
            var entries = new List<SrtEntry>();
            var lines = srtContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int i = 0;
            while (i < lines.Length)
            {
                // Skip empty lines
                while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                    i++;

                if (i >= lines.Length) break;

                // Parse index
                if (!int.TryParse(lines[i], out int index))
                {
                    i++;
                    continue;
                }

                i++;
                if (i >= lines.Length) break;

                // Parse time
                var timeMatch = _timeRegex.Match(lines[i]);
                if (!timeMatch.Success)
                {
                    i++;
                    continue;
                }

                var startTime = ParseTime(timeMatch.Groups[1].Value, timeMatch.Groups[2].Value,
                                         timeMatch.Groups[3].Value, timeMatch.Groups[4].Value);
                var endTime = ParseTime(timeMatch.Groups[5].Value, timeMatch.Groups[6].Value,
                                       timeMatch.Groups[7].Value, timeMatch.Groups[8].Value);

                i++;
                if (i >= lines.Length) break;

                // Parse text
                var textBuilder = new StringBuilder();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    textBuilder.AppendLine(lines[i]);
                    i++;
                }

                entries.Add(new SrtEntry
                {
                    Index = index,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = textBuilder.ToString().Trim()
                });
            }

            return entries;
        }

        public string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }

        public TimeSpan ParseTime(string timeString)
        {
            var match = Regex.Match(timeString, @"(\d{2}):(\d{2}):(\d{2}),(\d{3})");
            if (match.Success)
            {
                return ParseTime(match.Groups[1].Value, match.Groups[2].Value,
                               match.Groups[3].Value, match.Groups[4].Value);
            }
            return TimeSpan.Zero;
        }

        private TimeSpan ParseTime(string hours, string minutes, string seconds, string milliseconds)
        {
            return new TimeSpan(0, int.Parse(hours), int.Parse(minutes), int.Parse(seconds), int.Parse(milliseconds));
        }
    }
}