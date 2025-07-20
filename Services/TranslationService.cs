// Services/TranslationService.cs
using System.Text.RegularExpressions;

namespace Film_website.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TranslationService> _logger;

        public TranslationService(IConfiguration configuration, ILogger<TranslationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> TranslateSubtitleAsync(string subtitleText, string sourceLanguage, string targetLanguage)
        {
            try
            {
                // Mock translation - in production, use Google Translate API, Azure Translator, etc.
                var translatedText = await MockTranslateAsync(subtitleText, sourceLanguage, targetLanguage);
                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation");
                return subtitleText; // Return original if translation fails
            }
        }

        private async Task<string> MockTranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            await Task.Delay(1000); // Simulate API call delay

            // Simple mock translations for demonstration
            var translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["vi"] = text.Replace("Hello", "Xin chào")
                                .Replace("Thank you", "Cảm ơn")
                                .Replace("Goodbye", "Tạm biệt"),
                    ["es"] = text.Replace("Hello", "Hola")
                                .Replace("Thank you", "Gracias")
                                .Replace("Goodbye", "Adiós"),
                    ["fr"] = text.Replace("Hello", "Bonjour")
                                .Replace("Thank you", "Merci")
                                .Replace("Goodbye", "Au revoir")
                }
            };

            if (translations.ContainsKey(sourceLanguage) &&
                translations[sourceLanguage].ContainsKey(targetLanguage))
            {
                return translations[sourceLanguage][targetLanguage];
            }

            return $"[{targetLanguage.ToUpper()}] {text}";
        }
    }
}