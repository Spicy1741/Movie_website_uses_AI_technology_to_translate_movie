using System.ComponentModel.DataAnnotations;

namespace Film_website.Models
{
    public class SubtitleParagraphViewModel
    {
        public int Number { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Text { get; set; }
    }

    public class TranslateSubtitlesRequest
    {
        public List<SubtitleParagraphViewModel> Subtitles { get; set; } = new();
        public string SourceLanguage { get; set; } = "";
        public string TargetLanguage { get; set; } = "";
    }

    public class TranslateSubtitlesResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<SubtitleParagraphViewModel> TranslatedSubtitles { get; set; } = new();
        public string? SrtContent { get; set; }
    }

    public class LanguageOption
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}