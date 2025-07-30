using Newtonsoft.Json;
using System.Text;

namespace Film_website.Services
{
    public class GptTranslationService : IGptTranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GptTranslationService> _logger;
        private readonly string _apiKey;

        public GptTranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<GptTranslationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "auto")
        {
            try
            {
                var languageInstructions = GetLanguageInstructions(targetLanguage);

                var prompt = sourceLanguage == "auto"
                    ? $"Translate the following text to {targetLanguage}. {languageInstructions} Only return the translated text, no explanations:\n\n{text}"
                    : $"Translate the following text from {sourceLanguage} to {targetLanguage}. {languageInstructions} Only return the translated text, no explanations:\n\n{text}";

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = GetSystemPrompt(targetLanguage) },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 2000,
                    temperature = 0.3
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                _logger.LogInformation($"Starting translation to {targetLanguage}");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"GPT API error: {response.StatusCode} - {responseContent}");
                    return text; // Return original on error
                }

                dynamic? result = JsonConvert.DeserializeObject(responseContent);
                var translatedText = result?.choices?[0]?.message?.content?.ToString()?.Trim() ?? text;

                _logger.LogInformation("Translation completed successfully");
                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GPT translation");
                return text; // Return original text on error
            }
        }

        private string GetSystemPrompt(string targetLanguage)
        {
            return targetLanguage.ToLower() switch
            {
                "vietnamese" => "You are a professional translator specializing in Vietnamese. Provide accurate, natural-sounding Vietnamese translations that maintain the original meaning and context.",
                "spanish" => "You are a professional translator specializing in Spanish. Provide accurate, natural-sounding Spanish translations.",
                "french" => "You are a professional translator specializing in French. Provide accurate, natural-sounding French translations.",
                _ => "You are a professional translator. Provide accurate, natural-sounding translations that maintain the original meaning and context."
            };
        }

        private string GetLanguageInstructions(string targetLanguage)
        {
            return targetLanguage.ToLower() switch
            {
                "vietnamese" => "Ensure the Vietnamese translation uses proper grammar, natural phrasing, and appropriate formality level.",
                "spanish" => "Use neutral Spanish that is understood across different Spanish-speaking regions.",
                "french" => "Use standard French with proper grammar and natural phrasing.",
                _ => "Maintain natural phrasing and proper grammar in the target language."
            };
        }
    }
}