// Services/ITranslationService.cs
namespace Film_website.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateSubtitleAsync(string subtitleText, string sourceLanguage, string targetLanguage);
    }
}