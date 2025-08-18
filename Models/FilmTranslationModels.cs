namespace Film_website.Models
{
    // Enhanced TranscriptionResponse with timing validation
    public class TranscriptionResponse
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<SrtEntry> Segments { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string Message { get; set; } = string.Empty;

        // NEW: Timing validation properties
        public TimingValidationInfo TimingInfo { get; set; } = new();
    }

    // NEW: Timing validation information
    public class TimingValidationInfo
    {
        public double TotalDurationSeconds { get; set; }
        public double AverageSegmentDurationSeconds { get; set; }
        public double MinSegmentDurationSeconds { get; set; }
        public double MaxSegmentDurationSeconds { get; set; }
        public int OverlapCount { get; set; }
        public int GapCount { get; set; }
        public List<string> ValidationWarnings { get; set; } = new();
    }

    // Enhanced SrtEntry with additional timing properties
    public class SrtEntry
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;

        // NEW: Computed properties for validation
        public TimeSpan Duration => EndTime - StartTime;
        public double DurationSeconds => Duration.TotalSeconds;

        // NEW: Validation helper methods
        public bool IsValidDuration(double minSeconds = 0.5, double maxSeconds = 10.0)
        {
            return DurationSeconds >= minSeconds && DurationSeconds <= maxSeconds;
        }

        public bool OverlapsWith(SrtEntry other)
        {
            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        public TimeSpan GapBefore(SrtEntry previous)
        {
            return StartTime - previous.EndTime;
        }
    }

    // NEW: Video metadata model
    public class VideoMetadata
    {
        public double DurationSeconds { get; set; }
        public int AudioSampleRate { get; set; }
        public int AudioChannels { get; set; }
        public string VideoCodec { get; set; } = string.Empty;
        public string AudioCodec { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    // NEW: Audio metadata model
    public class AudioMetadata
    {
        public double DurationSeconds { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public string Codec { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int BitRate { get; set; }
    }

    // NEW: Processing statistics model
    public class ProcessingStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalProcessingTime => EndTime - StartTime;

        public TimeSpan AudioExtractionTime { get; set; }
        public TimeSpan TranscriptionTime { get; set; }
        public TimeSpan TranslationTime { get; set; }
        public TimeSpan SrtGenerationTime { get; set; }

        public VideoMetadata OriginalVideo { get; set; } = new();
        public AudioMetadata ExtractedAudio { get; set; } = new();
        public TimingValidationInfo TimingValidation { get; set; } = new();

        public int SegmentCount { get; set; }
        public int CharacterCount { get; set; }
        public string DetectedLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;

        public List<string> ProcessingWarnings { get; set; } = new();
        public List<string> TimingIssues { get; set; } = new();
    }
}