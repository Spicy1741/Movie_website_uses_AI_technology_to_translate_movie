// Models/WhisperModels.cs
using Microsoft.AspNetCore.Http;

namespace Film_website.Models
{
    public class TranslationRequest
    {
        public IFormFile? VideoFile { get; set; }
        public string SourceLanguage { get; set; } = "en";
        public string TargetLanguage { get; set; } = "es";
        public string OutputFormat { get; set; } = "srt";
        public bool EnableTranslation { get; set; } = true;
        public bool EnableDebugConsole { get; set; } = false;
        public string? Notes { get; set; }
    }

    public class TranslationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
        public string? DownloadUrl { get; set; }
        public string? FileName { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}