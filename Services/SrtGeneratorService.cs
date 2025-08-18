using System.Text;
using System.Text.RegularExpressions;
using Film_website.Models;

namespace Film_website.Services
{
    public class SrtGeneratorService : ISrtGeneratorService
    {
        private readonly ILogger<SrtGeneratorService> _logger;
        private static readonly Regex _timeRegex = new Regex(@"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");

        public SrtGeneratorService(ILogger<SrtGeneratorService> logger)
        {
            _logger = logger;
        }

        public string GenerateSrt(List<SrtEntry> entries)
        {
            if (entries == null || !entries.Any())
            {
                _logger.LogWarning("No entries provided for SRT generation");
                return string.Empty;
            }

            // FIXED: Validate and optimize entries before generating SRT
            var validatedEntries = ValidateAndOptimizeEntries(entries);

            var srt = new StringBuilder();

            foreach (var entry in validatedEntries)
            {
                srt.AppendLine(entry.Index.ToString());
                srt.AppendLine($"{FormatTime(entry.StartTime)} --> {FormatTime(entry.EndTime)}");
                srt.AppendLine(string.IsNullOrEmpty(entry.TranslatedText) ? entry.Text : entry.TranslatedText);
                srt.AppendLine();
            }

            _logger.LogInformation($"Generated SRT content with {validatedEntries.Count} entries");
            return srt.ToString();
        }

        // NEW: Comprehensive validation and optimization of SRT entries
        private List<SrtEntry> ValidateAndOptimizeEntries(List<SrtEntry> entries)
        {
            var validated = new List<SrtEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var newEntry = new SrtEntry
                {
                    Index = i + 1, // Ensure consecutive numbering
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Text = CleanSubtitleText(entry.Text),
                    TranslatedText = CleanSubtitleText(entry.TranslatedText)
                };

                // FIXED: Validate and fix timing issues
                newEntry = ValidateEntryTiming(newEntry, i > 0 ? validated[i - 1] : null);

                // Only add entries with valid text
                if (!string.IsNullOrWhiteSpace(newEntry.Text))
                {
                    validated.Add(newEntry);
                }
                else
                {
                    _logger.LogWarning($"Skipping entry {entry.Index} - empty text after cleaning");
                }
            }

            // FIXED: Final pass to ensure no overlaps
            validated = EnsureNoOverlaps(validated);

            _logger.LogInformation($"Validated {validated.Count} entries from {entries.Count} input entries");
            return validated;
        }

        // NEW: Validate individual entry timing
        private SrtEntry ValidateEntryTiming(SrtEntry entry, SrtEntry? previousEntry)
        {
            var validated = new SrtEntry
            {
                Index = entry.Index,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                Text = entry.Text,
                TranslatedText = entry.TranslatedText
            };

            // FIXED: Ensure minimum duration (0.5 seconds)
            var minDuration = TimeSpan.FromSeconds(0.5);
            if (validated.EndTime - validated.StartTime < minDuration)
            {
                validated.EndTime = validated.StartTime + minDuration;
                _logger.LogDebug($"Extended entry {entry.Index} duration to minimum {minDuration.TotalSeconds}s");
            }

            // FIXED: Ensure maximum duration (10 seconds for readability)
            var maxDuration = TimeSpan.FromSeconds(10);
            if (validated.EndTime - validated.StartTime > maxDuration)
            {
                validated.EndTime = validated.StartTime + maxDuration;
                _logger.LogDebug($"Shortened entry {entry.Index} duration to maximum {maxDuration.TotalSeconds}s");
            }

            // FIXED: Ensure no negative timestamps
            if (validated.StartTime < TimeSpan.Zero)
            {
                var offset = TimeSpan.Zero - validated.StartTime;
                validated.StartTime = TimeSpan.Zero;
                validated.EndTime += offset;
                _logger.LogDebug($"Adjusted entry {entry.Index} to prevent negative start time");
            }

            // FIXED: Prevent overlap with previous entry
            if (previousEntry != null)
            {
                var minGap = TimeSpan.FromMilliseconds(100); // 100ms minimum gap
                var requiredStartTime = previousEntry.EndTime + minGap;

                if (validated.StartTime < requiredStartTime)
                {
                    var shift = requiredStartTime - validated.StartTime;
                    validated.StartTime = requiredStartTime;
                    validated.EndTime += shift;
                    _logger.LogDebug($"Shifted entry {entry.Index} by {shift.TotalMilliseconds}ms to prevent overlap");
                }
            }

            return validated;
        }

        // NEW: Ensure no overlaps in the final list
        private List<SrtEntry> EnsureNoOverlaps(List<SrtEntry> entries)
        {
            if (entries.Count <= 1) return entries;

            var noOverlaps = new List<SrtEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                var current = entries[i];

                // Check for overlap with next entry
                if (i < entries.Count - 1)
                {
                    var next = entries[i + 1];
                    if (current.EndTime > next.StartTime)
                    {
                        // Adjust current entry's end time
                        var gap = TimeSpan.FromMilliseconds(100);
                        current.EndTime = next.StartTime - gap;

                        // Ensure we don't make it too short
                        if (current.EndTime <= current.StartTime)
                        {
                            current.EndTime = current.StartTime + TimeSpan.FromSeconds(0.5);

                            // If this creates an overlap, adjust the next entry's start
                            if (current.EndTime > next.StartTime && i < entries.Count - 1)
                            {
                                entries[i + 1].StartTime = current.EndTime + gap;
                                var minEndTime = entries[i + 1].StartTime.Add(TimeSpan.FromSeconds(0.5));
                                if (entries[i + 1].EndTime < minEndTime)
                                {
                                    entries[i + 1].EndTime = minEndTime;
                                }
                            }
                        }

                        _logger.LogDebug($"Resolved overlap between entries {current.Index} and {next.Index}");
                    }
                }

                noOverlaps.Add(current);
            }

            return noOverlaps;
        }

        // NEW: Clean subtitle text for better readability
        private string CleanSubtitleText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove extra whitespace and normalize line endings
            text = Regex.Replace(text.Trim(), @"\s+", " ");

            // Remove common transcription artifacts
            text = Regex.Replace(text, @"\[.*?\]", ""); // Remove [background noise], [music], etc.
            text = Regex.Replace(text, @"\(.*?\)", ""); // Remove (inaudible), (crosstalk), etc.

            // Fix common punctuation issues
            text = Regex.Replace(text, @"\s+([,.!?;:])", "$1"); // Remove space before punctuation
            text = Regex.Replace(text, @"([.!?])\s*([a-z])", "$1 $2"); // Ensure space after sentence ending

            // Capitalize first letter of sentences
            text = Regex.Replace(text, @"(^|[.!?]\s+)([a-z])",
                match => match.Groups[1].Value + match.Groups[2].Value.ToUpper());

            // Ensure first character is capitalized
            if (text.Length > 0)
            {
                text = char.ToUpper(text[0]) + text.Substring(1);
            }

            return text.Trim();
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

                var cleanedText = CleanSubtitleText(textBuilder.ToString());
                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    entries.Add(new SrtEntry
                    {
                        Index = index,
                        StartTime = startTime,
                        EndTime = endTime,
                        Text = cleanedText
                    });
                }
            }

            _logger.LogInformation($"Parsed {entries.Count} entries from SRT content");

            // FIXED: Validate parsed entries
            return ValidateAndOptimizeEntries(entries);
        }

        public string FormatTime(TimeSpan time)
        {
            // FIXED: Ensure proper millisecond precision and format
            var totalMs = (int)time.TotalMilliseconds;
            var hours = totalMs / 3600000;
            var minutes = (totalMs % 3600000) / 60000;
            var seconds = (totalMs % 60000) / 1000;
            var milliseconds = totalMs % 1000;

            return $"{hours:00}:{minutes:00}:{seconds:00},{milliseconds:000}";
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
            try
            {
                var h = int.Parse(hours);
                var m = int.Parse(minutes);
                var s = int.Parse(seconds);
                var ms = int.Parse(milliseconds);

                // FIXED: More precise time calculation
                return new TimeSpan(0, h, m, s, ms);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error parsing time: {hours}:{minutes}:{seconds},{milliseconds}");
                return TimeSpan.Zero;
            }
        }

        // PRIVATE: Method to validate SRT timing consistency
        private bool ValidateSrtTimingInternal(List<SrtEntry> entries, out List<string> issues)
        {
            issues = new List<string>();

            if (entries == null || !entries.Any())
            {
                issues.Add("No entries to validate");
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Check for valid duration
                if (entry.EndTime <= entry.StartTime)
                {
                    issues.Add($"Entry {entry.Index}: Invalid duration - end time must be after start time");
                }

                // Check for reasonable duration
                var duration = entry.EndTime - entry.StartTime;
                if (duration.TotalSeconds < 0.1)
                {
                    issues.Add($"Entry {entry.Index}: Duration too short ({duration.TotalSeconds:F3}s)");
                }
                else if (duration.TotalSeconds > 15)
                {
                    issues.Add($"Entry {entry.Index}: Duration very long ({duration.TotalSeconds:F1}s) - consider splitting");
                }

                // Check for overlaps with next entry
                if (i < entries.Count - 1)
                {
                    var nextEntry = entries[i + 1];
                    if (entry.EndTime > nextEntry.StartTime)
                    {
                        var overlap = entry.EndTime - nextEntry.StartTime;
                        issues.Add($"Entry {entry.Index}: Overlaps with next entry by {overlap.TotalMilliseconds:F0}ms");
                    }
                }

                // Check for large gaps
                if (i > 0)
                {
                    var prevEntry = entries[i - 1];
                    var gap = entry.StartTime - prevEntry.EndTime;
                    if (gap.TotalSeconds > 5)
                    {
                        issues.Add($"Entry {entry.Index}: Large gap ({gap.TotalSeconds:F1}s) from previous entry");
                    }
                }
            }

            return issues.Count == 0;
        }
    }
}