using System.ComponentModel.DataAnnotations;

namespace Film_website.Models
{
    public class TranscriptionRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public string Language { get; set; } = "auto";
    }

    public class TranscriptionResponse
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<SrtEntry> Segments { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TranslationRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = "auto";
    }

    public class SrtEntry
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
    }
}