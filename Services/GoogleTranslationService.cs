using Film_website.Models;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Film_website.Services
{
    public interface IGoogleTranslationService
    {
        Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage);
        Task<List<SubtitleParagraphViewModel>> TranslateSubtitlesAsync(
            List<SubtitleParagraphViewModel> subtitles,
            string sourceLanguage,
            string targetLanguage);
    }

    public class GoogleTranslationService : IGoogleTranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleTranslationService> _logger;

        public GoogleTranslationService(HttpClient httpClient, ILogger<GoogleTranslationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure HttpClient for Google Translate
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            try
            {
                // Clean the text for translation
                var cleanText = CleanTextForTranslation(text);

                // Use Google Translate free API endpoint
                var encodedText = HttpUtility.UrlEncode(cleanText);
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={encodedText}";

                var response = await _httpClient.GetStringAsync(url);
                var translatedText = ParseGoogleTranslateResponse(response);

                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translation failed for text: {Text}", text.Substring(0, Math.Min(50, text.Length)));
                return text; // Return original text if translation fails
            }
        }

        public async Task<List<SubtitleParagraphViewModel>> TranslateSubtitlesAsync(
            List<SubtitleParagraphViewModel> subtitles,
            string sourceLanguage,
            string targetLanguage)
        {
            var translatedSubtitles = new List<SubtitleParagraphViewModel>();
            var total = subtitles.Count;

            for (int i = 0; i < subtitles.Count; i++)
            {
                var subtitle = subtitles[i];

                try
                {
                    var translatedText = await TranslateTextAsync(subtitle.Text ?? "", sourceLanguage, targetLanguage);

                    translatedSubtitles.Add(new SubtitleParagraphViewModel
                    {
                        Number = subtitle.Number,
                        StartTime = subtitle.StartTime,
                        EndTime = subtitle.EndTime,
                        Text = translatedText
                    });

                    // Add delay to avoid rate limiting
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error translating subtitle {Number}", subtitle.Number);

                    translatedSubtitles.Add(new SubtitleParagraphViewModel
                    {
                        Number = subtitle.Number,
                        StartTime = subtitle.StartTime,
                        EndTime = subtitle.EndTime,
                        Text = subtitle.Text + " [Translation Error]"
                    });
                }
            }

            return translatedSubtitles;
        }

        private string CleanTextForTranslation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove HTML tags
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");

            // Remove subtitle formatting
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\{[^}]+\}", "");

            // Clean up whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        private string ParseGoogleTranslateResponse(string response)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(response);
                var translations = jsonDoc.RootElement[0];

                var result = new StringBuilder();
                foreach (var translation in translations.EnumerateArray())
                {
                    if (translation.GetArrayLength() > 0)
                    {
                        result.Append(translation[0].GetString());
                    }
                }

                return result.ToString();
            }
            catch
            {
                return response; // Fallback to original response
            }
        }
    }
}