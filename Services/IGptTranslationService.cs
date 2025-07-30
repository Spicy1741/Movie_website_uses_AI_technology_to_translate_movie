namespace Film_website.Services
{
    public interface IGptTranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "auto");
    }
}