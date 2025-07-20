using Film_website.Models;

namespace Film_website.Services
{
    public interface ISubtitleService
    {
        Task<SubtitleParseResult> ParseSubtitleFileAsync(IFormFile file);
        Task<SubtitleParseResult> ParseSubtitleTextAsync(string content);
        string GenerateSrtContent(List<SubtitleParagraphViewModel> paragraphs);
        List<LanguageOption> GetSupportedLanguages();
        bool IsValidSubtitleFile(string fileName);
    }

    public class SubtitleParseResult
    {
        public bool Success { get; set; }
        public List<SubtitleParagraphViewModel> Paragraphs { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}