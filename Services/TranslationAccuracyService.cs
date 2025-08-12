using Film_website.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Film_website.Services
{
    public class TranslationAccuracyService : ITranslationAccuracyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TranslationAccuracyService> _logger;
        private readonly string _apiKey;

        public TranslationAccuracyService(HttpClient httpClient, IConfiguration configuration, ILogger<TranslationAccuracyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<TranslationAccuracyResult> CheckTranslationAccuracyAsync(
            string originalFilePath,
            string translatedFilePath,
            string sourceLanguage,
            string targetLanguage,
            string targetCountry = "")
        {
            try
            {
                _logger.LogInformation($"Starting translation accuracy check for {originalFilePath} -> {translatedFilePath}");

                // Read both files
                var originalTexts = await ReadSubtitleFileAsync(originalFilePath);
                var translatedTexts = await ReadSubtitleFileAsync(translatedFilePath);

                if (originalTexts.Count != translatedTexts.Count)
                {
                    _logger.LogWarning($"Mismatch in sentence count: Original({originalTexts.Count}) vs Translated({translatedTexts.Count})");
                }

                var result = new TranslationAccuracyResult
                {
                    SentenceResults = new List<SentenceAccuracy>()
                };

                var sentenceResults = new List<SentenceAccuracy>();
                var totalSimilarity = 0.0;
                var totalAccuracy = 0;
                var processedCount = 0;

                // Process each sentence pair
                for (int i = 0; i < Math.Min(originalTexts.Count, translatedTexts.Count); i++)
                {
                    var originalText = originalTexts[i].Trim();
                    var translatedText = translatedTexts[i].Trim();

                    if (string.IsNullOrWhiteSpace(originalText) || string.IsNullOrWhiteSpace(translatedText))
                        continue;

                    var sentenceResult = new SentenceAccuracy
                    {
                        Index = i + 1,
                        OriginalText = originalText,
                        TranslatedText = translatedText
                    };

                    try
                    {
                        // Layer 1: Semantic similarity
                        _logger.LogInformation($"Processing sentence {i + 1}: Layer 1 - Semantic similarity");
                        sentenceResult.SemanticSimilarity = await CalculateSemanticSimilarityAsync(originalText, translatedText);
                        totalSimilarity += sentenceResult.SemanticSimilarity;

                        // Layer 2: Accuracy check (only if similarity >= 0.8)
                        if (sentenceResult.SemanticSimilarity >= 0.8)
                        {
                            _logger.LogInformation($"Processing sentence {i + 1}: Layer 2 - Accuracy check");
                            var accuracyRequest = new AccuracyCheckRequest
                            {
                                OriginalText = originalText,
                                TranslatedText = translatedText,
                                SourceLanguage = sourceLanguage,
                                TargetLanguage = targetLanguage
                            };

                            var accuracyResponse = await CheckAccuracyWithGptAsync(accuracyRequest);
                            sentenceResult.AccuracyScore = accuracyResponse.Score;
                            sentenceResult.Feedback = accuracyResponse.Feedback;
                            totalAccuracy += accuracyResponse.Score;
                        }
                        else
                        {
                            sentenceResult.AccuracyScore = 0;
                            sentenceResult.Feedback = "Low semantic similarity - requires manual review";
                        }

                        // Layer 3: Cultural sensitivity check
                        _logger.LogInformation($"Processing sentence {i + 1}: Layer 3 - Cultural sensitivity");
                        var culturalRequest = new CulturalCheckRequest
                        {
                            OriginalText = originalText,
                            TranslatedText = translatedText,
                            TargetCountry = targetCountry,
                            TargetLanguage = targetLanguage
                        };

                        var culturalResponse = await CheckCulturalSensitivityAsync(culturalRequest);
                        sentenceResult.CulturalRisk = culturalResponse.Risk;
                        sentenceResult.CulturalComment = culturalResponse.Comment;
                        sentenceResult.CulturalSuggestion = culturalResponse.Suggestion;
                        sentenceResult.SensitiveWords = culturalResponse.SensitiveWords;
                        sentenceResult.IsSensitive = culturalResponse.Risk != "Safe";

                        if (sentenceResult.IsSensitive)
                        {
                            result.HasSensitiveContent = true;
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing sentence {i + 1}");
                        sentenceResult.Feedback = $"Error during processing: {ex.Message}";
                    }

                    sentenceResults.Add(sentenceResult);

                    // Add small delay to avoid rate limiting
                    await Task.Delay(100);
                }

                // Calculate overall results
                if (processedCount > 0)
                {
                    result.SemanticSimilarity = totalSimilarity / processedCount;
                    result.AccuracyScore = (int)(totalAccuracy / processedCount);
                }

                result.SentenceResults = sentenceResults;

                // Overall cultural risk assessment
                var riskCounts = sentenceResults.GroupBy(s => s.CulturalRisk)
                    .ToDictionary(g => g.Key, g => g.Count());

                if (riskCounts.ContainsKey("Inappropriate") && riskCounts["Inappropriate"] > 0)
                {
                    result.CulturalRisk = "Inappropriate";
                    result.CulturalComment = $"Found {riskCounts["Inappropriate"]} inappropriate content(s) that require immediate attention.";
                }
                else if (riskCounts.ContainsKey("Potentially sensitive") && riskCounts["Potentially sensitive"] > 0)
                {
                    result.CulturalRisk = "Potentially sensitive";
                    result.CulturalComment = $"Found {riskCounts["Potentially sensitive"]} potentially sensitive content(s) that may need review.";
                }
                else
                {
                    result.CulturalRisk = "Safe";
                    result.CulturalComment = "No cultural or political sensitivity issues detected.";
                }

                // Generate overall feedback
                var sensitiveCount = sentenceResults.Count(s => s.IsSensitive);
                var lowAccuracyCount = sentenceResults.Count(s => s.AccuracyScore < 70);

                var feedbackParts = new List<string>();
                if (lowAccuracyCount > 0)
                    feedbackParts.Add($"{lowAccuracyCount} sentence(s) have accuracy below 70%");
                if (sensitiveCount > 0)
                    feedbackParts.Add($"{sensitiveCount} sentence(s) contain sensitive content");

                result.AccuracyFeedback = feedbackParts.Any()
                    ? string.Join(", ", feedbackParts) + ". Manual review recommended."
                    : "Translation quality looks good overall.";

                _logger.LogInformation($"Translation accuracy check completed. Overall similarity: {result.SemanticSimilarity:F2}, Accuracy: {result.AccuracyScore}%");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation accuracy check");
                throw;
            }
        }

        public async Task<double> CalculateSemanticSimilarityAsync(string originalText, string translatedText)
        {
            try
            {
                // Add detailed logging
                _logger.LogInformation($"=== EMBEDDING DEBUG ===");
                _logger.LogInformation($"Original text: '{originalText}' (Length: {originalText?.Length})");
                _logger.LogInformation($"Translated text: '{translatedText}' (Length: {translatedText?.Length})");

                // Validate inputs
                if (string.IsNullOrWhiteSpace(originalText) || string.IsNullOrWhiteSpace(translatedText))
                {
                    _logger.LogWarning("Empty or null text provided for similarity calculation");
                    return 0.0;
                }

                var requestBody = new OpenAIEmbeddingRequest
                {
                    Model = "text-embedding-3-large",
                    Input = new List<string> { originalText.Trim(), translatedText.Trim() },
                    Encoding_format = "float"
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending embedding request for texts: '{originalText.Substring(0, Math.Min(50, originalText.Length))}...'");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Embedding API Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Embedding API error: {response.StatusCode} - {responseContent}");
                    return 0.0;
                }

                var embeddingResponse = JsonConvert.DeserializeObject<OpenAIEmbeddingResponse>(responseContent);

                if (embeddingResponse?.Data?.Count >= 2)
                {
                    var embedding1 = embeddingResponse.Data[0].Embedding;
                    var embedding2 = embeddingResponse.Data[1].Embedding;

                    _logger.LogInformation($"Embeddings received - Vector 1 length: {embedding1.Count}, Vector 2 length: {embedding2.Count}");

                    var similarity = CalculateCosineSimilarity(embedding1, embedding2);
                    _logger.LogInformation($"Calculated similarity: {similarity:F4}");

                    return similarity;
                }
                else
                {
                    _logger.LogError($"Invalid embedding response: Data count = {embeddingResponse?.Data?.Count}");
                    return 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating semantic similarity");
                return 0.0;
            }
        }

        public async Task<AccuracyCheckResponse> CheckAccuracyWithGptAsync(AccuracyCheckRequest request)
        {
            try
            {
                var prompt = $@"You are a professional translation quality assessor. Please evaluate the accuracy of this translation:

Original ({request.SourceLanguage}): ""{request.OriginalText}""
Translation ({request.TargetLanguage}): ""{request.TranslatedText}""

Please provide:
1. An accuracy score from 0-100 (where 100 is perfect translation)
2. Brief feedback explaining your assessment

Focus on:
- Semantic accuracy (meaning preservation)
- Grammatical correctness
- Natural language flow
- Context appropriateness

Respond in this JSON format:
{{
    ""score"": <number 0-100>,
    ""feedback"": ""<brief explanation>""
}}";

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional translation quality assessor. Always respond with valid JSON." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 300,
                    temperature = 0.2
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"GPT API error for accuracy check: {response.StatusCode} - {responseContent}");
                    return new AccuracyCheckResponse { Score = 0, Feedback = "API error during accuracy check" };
                }

                dynamic? result = JsonConvert.DeserializeObject(responseContent);
                var gptResponse = result?.choices?[0]?.message?.content?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(gptResponse))
                {
                    var accuracyResponse = JsonConvert.DeserializeObject<AccuracyCheckResponse>(gptResponse);
                    return accuracyResponse ?? new AccuracyCheckResponse { Score = 0, Feedback = "Failed to parse response" };
                }

                return new AccuracyCheckResponse { Score = 0, Feedback = "No response from GPT" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GPT accuracy check");
                return new AccuracyCheckResponse { Score = 0, Feedback = $"Error: {ex.Message}" };
            }
        }

        public async Task<CulturalCheckResponse> CheckCulturalSensitivityAsync(CulturalCheckRequest request)
        {
            try
            {
                var countryContext = !string.IsNullOrEmpty(request.TargetCountry)
                    ? $" for {request.TargetCountry}"
                    : "";

                var prompt = $@"You are a cultural sensitivity expert. Please analyze this translation for cultural and political appropriateness{countryContext}:

Original text: ""{request.OriginalText}""
Translated text: ""{request.TranslatedText}""
Target language: {request.TargetLanguage}

Please assess for:
- Cultural sensitivity and appropriateness
- Political correctness
- Religious sensitivity
- Social taboos
- Potentially offensive content

Respond in this JSON format:
{{
    ""risk"": ""<Safe|Potentially sensitive|Inappropriate>"",
    ""comment"": ""<explanation of any issues found>"",
    ""suggestion"": ""<alternative suggestion if needed>"",
    ""sensitiveWords"": [""<word1>"", ""<word2>""]
}}

If no issues found, use ""Safe"" for risk and empty arrays for sensitiveWords.";

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a cultural sensitivity expert. Always respond with valid JSON." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 400,
                    temperature = 0.1
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"GPT API error for cultural check: {response.StatusCode} - {responseContent}");
                    return new CulturalCheckResponse
                    {
                        Risk = "Safe",
                        Comment = "API error during cultural check",
                        Suggestion = "",
                        SensitiveWords = new List<string>()
                    };
                }

                dynamic? result = JsonConvert.DeserializeObject(responseContent);
                var gptResponse = result?.choices?[0]?.message?.content?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(gptResponse))
                {
                    var culturalResponse = JsonConvert.DeserializeObject<CulturalCheckResponse>(gptResponse);
                    return culturalResponse ?? new CulturalCheckResponse
                    {
                        Risk = "Safe",
                        Comment = "Failed to parse response",
                        Suggestion = "",
                        SensitiveWords = new List<string>()
                    };
                }

                return new CulturalCheckResponse
                {
                    Risk = "Safe",
                    Comment = "No response from GPT",
                    Suggestion = "",
                    SensitiveWords = new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cultural sensitivity check");
                return new CulturalCheckResponse
                {
                    Risk = "Safe",
                    Comment = $"Error: {ex.Message}",
                    Suggestion = "",
                    SensitiveWords = new List<string>()
                };
            }
        }

        public async Task<List<string>> ReadSubtitleFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogError($"Subtitle file not found: {filePath}");
                    return new List<string>();
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                var sentences = new List<string>();

                if (Path.GetExtension(filePath).ToLower() == ".srt")
                {
                    // Parse SRT format
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // Skip empty lines, sequence numbers, and timestamps
                        if (string.IsNullOrEmpty(line) ||
                            Regex.IsMatch(line, @"^\d+$") ||
                            Regex.IsMatch(line, @"^\d{2}:\d{2}:\d{2},\d{3}\s*-->\s*\d{2}:\d{2}:\d{2},\d{3}$"))
                        {
                            continue;
                        }

                        // This is subtitle text
                        sentences.Add(line);
                    }
                }
                else
                {
                    // Assume plain text format
                    sentences = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                }

                _logger.LogInformation($"Read {sentences.Count} sentences from {filePath}");
                return sentences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading subtitle file: {filePath}");
                return new List<string>();
            }
        }

        private double CalculateCosineSimilarity(List<double> vector1, List<double> vector2)
        {
            if (vector1.Count != vector2.Count)
                return 0.0;

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < vector1.Count; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
                return 0.0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}