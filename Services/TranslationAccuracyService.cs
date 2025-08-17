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

                _logger.LogInformation($"Read {originalTexts.Count} sentences from {originalFilePath}");
                _logger.LogInformation($"Read {translatedTexts.Count} sentences from {translatedFilePath}");

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
                var successfulSimilarityCount = 0;  // 🆕 Only count successful similarity calculations
                var successfulAccuracyCount = 0;

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

                        // Only include successful similarity calculations in average
                        if (sentenceResult.SemanticSimilarity >= 0)
                        {
                            totalSimilarity += sentenceResult.SemanticSimilarity;
                            successfulSimilarityCount++;
                            _logger.LogInformation($"Sentence {i + 1}: Valid similarity = {sentenceResult.SemanticSimilarity:F3}");
                        }
                        else
                        {
                            _logger.LogWarning($"Sentence {i + 1}: Failed similarity calculation (result: {sentenceResult.SemanticSimilarity})");
                        }

                        // 🆕 Only include successful similarity calculations in average
                        if (sentenceResult.SemanticSimilarity >= 0) // 🆕 Accept 0 as valid similarity, exclude -1 failures
                        {
                            totalSimilarity += sentenceResult.SemanticSimilarity;
                            successfulSimilarityCount++;
                            _logger.LogInformation($"Sentence {i + 1}: Valid similarity = {sentenceResult.SemanticSimilarity:F3}");
                        }
                        else
                        {
                            _logger.LogWarning($"Sentence {i + 1}: Failed similarity calculation (result: {sentenceResult.SemanticSimilarity})");
                        }

                        // Layer 2: Accuracy check (only if similarity >= 0.8)
                        if (sentenceResult.SemanticSimilarity >= 0.2)
                        {
                            _logger.LogInformation($"Processing sentence {i + 1}: Layer 2 - Accuracy check (High confidence)");
                            var accuracyRequest = new AccuracyCheckRequest
                            {
                                OriginalText = originalText,
                                TranslatedText = translatedText,
                                SourceLanguage = sourceLanguage,
                                TargetLanguage = targetLanguage
                            };

                            var accuracyResult = await CheckAccuracyWithGptAsync(accuracyRequest);
                            sentenceResult.AccuracyScore = accuracyResult.Score;
                            sentenceResult.Feedback = accuracyResult.Feedback;

                            // 🆕 Only include successful accuracy calculations in average
                            if (accuracyResult.Score > 0)
                            {
                                totalAccuracy += accuracyResult.Score;
                                successfulAccuracyCount++;
                                _logger.LogInformation($"Sentence {i + 1}: Valid accuracy = {accuracyResult.Score}%");
                            }
                            else
                            {
                                _logger.LogWarning($"Sentence {i + 1}: Failed accuracy calculation (result: {accuracyResult.Score})");
                            }
                        }
                        else
                        {

                            // 🆕 Only include successful accuracy calculations in average
                            if (accuracyResult.Score > 0)
                            {
                                totalAccuracy += accuracyResult.Score;
                                successfulAccuracyCount++;
                                _logger.LogInformation($"Sentence {i + 1}: Valid accuracy = {accuracyResult.Score}%");
                            }
                            else
                            {
                                _logger.LogWarning($"Sentence {i + 1}: Failed accuracy calculation (result: {accuracyResult.Score})");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Processing sentence {i + 1}: Layer 2 - Accuracy check (Low confidence - using similarity-based score)");
                            var estimatedAccuracy = (int)(sentenceResult.SemanticSimilarity * 100);
                            sentenceResult.AccuracyScore = Math.Max(estimatedAccuracy, 20);
                            sentenceResult.Feedback = $"Low semantic similarity ({sentenceResult.SemanticSimilarity:P1}) - estimated accuracy based on similarity";

                            // 🆕 Include estimated accuracy in count (these are valid calculations)
                            totalAccuracy += sentenceResult.AccuracyScore;
                            successfulAccuracyCount++;
                            _logger.LogInformation($"Sentence {i + 1}: Estimated accuracy = {sentenceResult.AccuracyScore}%");
                            successfulAccuracyCount++;
                            _logger.LogInformation($"Sentence {i + 1}: Estimated accuracy = {sentenceResult.AccuracyScore}%");
                        }

                        // 🆕 Only add sentences that had successful processing
                        if (sentenceResult.SemanticSimilarity > 0 || sentenceResult.AccuracyScore > 0)
                        {
                            sentenceResults.Add(sentenceResult);
                        }

                   
                        if (sentenceResult.SemanticSimilarity > 0 || sentenceResult.AccuracyScore > 0)
                        {
                            sentenceResults.Add(sentenceResult);
                        }



                        // Layer 3: Cultural sensitivity
                        _logger.LogInformation($"Processing sentence {i + 1}: Layer 3 - Cultural sensitivity");
                        var culturalRequest = new CulturalCheckRequest
                        {
                            OriginalText = originalText,
                            TranslatedText = translatedText,
                            TargetLanguage = targetLanguage,
                            TargetCountry = targetCountry
                        };

                        var culturalResult = await CheckCulturalSensitivityAsync(culturalRequest);
                        sentenceResult.CulturalRisk = culturalResult.Risk;
                        sentenceResult.CulturalComment = culturalResult.Comment;
                        sentenceResult.CulturalSuggestion = culturalResult.Suggestion;
                        sentenceResult.SensitiveWords = culturalResult.SensitiveWords;
                        sentenceResult.IsSensitive = culturalResult.Risk != "Safe";

                        
                       
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing sentence {i + 1}");
                        sentenceResult.Feedback = $"Error during processing: {ex.Message}";
                    }

                   
                }

                // Calculate overall results
                result.SentenceResults = sentenceResults;
                result.SemanticSimilarity = successfulSimilarityCount > 0 ? totalSimilarity / successfulSimilarityCount : 0;
                result.AccuracyScore = successfulAccuracyCount > 0 ? (int)(totalAccuracy / successfulAccuracyCount) : 0;
                result.HasSensitiveContent = sentenceResults.Any(s => s.IsSensitive);

                // Aggregate cultural risks
                var culturalRisks = sentenceResults.Where(s => s.CulturalRisk != "Safe").ToList();
                if (culturalRisks.Any())
                {
                    var highestRisk = culturalRisks.Any(r => r.CulturalRisk == "Inappropriate") ? "Inappropriate" :
                                     culturalRisks.Any(r => r.CulturalRisk == "Potentially sensitive") ? "Potentially sensitive" : "Safe";
                    result.CulturalRisk = highestRisk;
                    result.CulturalComment = string.Join("; ", culturalRisks.Select(r => r.CulturalComment).Distinct());
                }
                else
                {
                    result.CulturalRisk = "Safe";
                    result.CulturalComment = "No cultural sensitivity issues detected";
                }

                // Generate overall feedback
                var feedbackParts = new List<string>();
                if (result.SemanticSimilarity < 0.7) feedbackParts.Add("low semantic similarity");
                if (result.AccuracyScore < 70) feedbackParts.Add("accuracy concerns");
                if (result.HasSensitiveContent) feedbackParts.Add("cultural sensitivity issues");

                result.AccuracyFeedback = feedbackParts.Any() ?
                    string.Join(", ", feedbackParts) + ". Manual review recommended."
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
                    return -1.0; // 🆕 Use -1 to indicate invalid input
                }

                // FIX: Use proper JSON structure with lowercase property names for OpenAI API
                var requestBody = new
                {
                    model = "text-embedding-3-large",
                    input = new string[] { originalText.Trim(), translatedText.Trim() },
                    encoding_format = "float"
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending embedding request for texts: '{originalText.Substring(0, Math.Min(50, originalText.Length))}...'");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Embedding API Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Embedding API error: {response.StatusCode} - {responseContent}");
                    return -1.0; // 🆕 Use -1 to indicate API failure
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
                return -1.0; // 🆕 Use -1 to indicate calculation failure
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

Respond ONLY with valid JSON in this exact format:
{{
    ""score"": <number 0-100>,
    ""feedback"": ""<brief explanation>""
}}";

                var requestBody = new
                {
                    model = "gpt-4.1-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional translation quality assessor. Always respond with valid JSON only. Do not include any text before or after the JSON." },
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
                    return new AccuracyCheckResponse { Score = -1, Feedback = "API error during accuracy check" };
                    //  Use -1 to indicate API failure
                }

                dynamic? result = JsonConvert.DeserializeObject(responseContent);
                var gptResponse = result?.choices?[0]?.message?.content?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(gptResponse))
                {
                    // FIX: Clean the response and ensure it's valid JSON
                    var cleanResponse = CleanJsonResponse(gptResponse);
                    try
                    {
                        var accuracyResponse = JsonConvert.DeserializeObject<AccuracyCheckResponse>(cleanResponse);
                        return accuracyResponse ?? new AccuracyCheckResponse { Score = 0, Feedback = "Failed to parse response" };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"JSON parsing error for accuracy response: {ex.Message}");
                        return new AccuracyCheckResponse { Score = -1, Feedback = "JSON parsing error" };
                        // 🆕 Use -1 to indicate parsing failure
                    }
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

Respond ONLY with valid JSON in this exact format:
{{
    ""risk"": ""<Safe|Potentially sensitive|Inappropriate>"",
    ""comment"": ""<explanation of any issues found>"",
    ""suggestion"": ""<alternative suggestion if needed>"",
    ""sensitiveWords"": [""<word1>"", ""<word2>""]
}}

If no issues found, use ""Safe"" for risk and empty arrays for sensitiveWords.";

                var requestBody = new
                {
                    model = "gpt-4.1",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a cultural sensitivity expert. Always respond with valid JSON only. Do not include any text before or after the JSON." },
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
                    // FIX: Clean the response and ensure it's valid JSON
                    var cleanResponse = CleanJsonResponse(gptResponse);
                    try
                    {
                        var culturalResponse = JsonConvert.DeserializeObject<CulturalCheckResponse>(cleanResponse);
                        return culturalResponse ?? new CulturalCheckResponse
                        {
                            Risk = "Safe",
                            Comment = "Failed to parse response",
                            Suggestion = "",
                            SensitiveWords = new List<string>()
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"JSON parsing error for cultural response: {ex.Message}. Response: {cleanResponse}");
                        // Return safe default if parsing fails
                        return new CulturalCheckResponse
                        {
                            Risk = "Safe",
                            Comment = "Response parsing failed - manual review recommended",
                            Suggestion = "",
                            SensitiveWords = new List<string>()
                        };
                    }
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

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Skip empty lines, SRT indices, and timecodes
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        Regex.IsMatch(trimmedLine, @"^\d+$") ||
                        Regex.IsMatch(trimmedLine, @"^\d{2}:\d{2}:\d{2},\d{3}\s*-->\s*\d{2}:\d{2}:\d{2},\d{3}$"))
                    {
                        continue;
                    }

                    sentences.Add(trimmedLine);
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

        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "{}";

            // Remove any text before the first { and after the last }
            var startIndex = response.IndexOf('{');
            var endIndex = response.LastIndexOf('}');

            if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
            {
                return response.Substring(startIndex, endIndex - startIndex + 1);
            }

            return "{}";
        }

        private AccuracyCheckResponse ExtractAccuracyFromText(string text)
        {
            try
            {
                // Try to extract score and feedback from plain text response
                var scoreMatch = Regex.Match(text, @"score["":\s]*(\d+)", RegexOptions.IgnoreCase);
                var score = scoreMatch.Success ? int.Parse(scoreMatch.Groups[1].Value) : 50;

                var feedbackMatch = Regex.Match(text, @"feedback["":\s]*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                var feedback = feedbackMatch.Success ? feedbackMatch.Groups[1].Value : "Could not parse detailed feedback";

                return new AccuracyCheckResponse
                {
                    Score = Math.Max(0, Math.Min(100, score)),
                    Feedback = feedback
                };
            }
            catch
            {
                return new AccuracyCheckResponse
                {
                    Score = 50,
                    Feedback = "Could not parse accuracy response"
                };
            }
        }
    }
}