using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Film_website.Models
{
    // Main result model for the complete accuracy check
    public class TranslationAccuracyResult
    {
        public double SemanticSimilarity { get; set; }
        public int AccuracyScore { get; set; }
        public string AccuracyFeedback { get; set; } = string.Empty;
        public string CulturalRisk { get; set; } = string.Empty; // "Safe", "Potentially sensitive", "Inappropriate"
        public string CulturalComment { get; set; } = string.Empty;
        public string CulturalSuggestion { get; set; } = string.Empty;
        public List<SentenceAccuracy> SentenceResults { get; set; } = new();
        public bool HasSensitiveContent { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    // Individual sentence accuracy result
    public class SentenceAccuracy
    {
        public int Index { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public double SemanticSimilarity { get; set; }
        public int AccuracyScore { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string CulturalRisk { get; set; } = string.Empty;
        public string CulturalComment { get; set; } = string.Empty;
        public string CulturalSuggestion { get; set; } = string.Empty;
        public bool IsSensitive { get; set; }
        public List<string> SensitiveWords { get; set; } = new();
    }

    // API request/response models for OpenAI Embeddings
    public class OpenAIEmbeddingRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "text-embedding-3-large";

        [JsonProperty("input")]
        public List<string> Input { get; set; } = new();

        [JsonProperty("encoding_format")]
        public string EncodingFormat { get; set; } = "float";
    }

    public class OpenAIEmbeddingResponse
    {
        [JsonProperty("data")]
        public List<EmbeddingData> Data { get; set; } = new();

        [JsonProperty("usage")]
        public EmbeddingUsage Usage { get; set; } = new();
    }

    public class EmbeddingData
    {
        [JsonProperty("embedding")]
        public List<double> Embedding { get; set; } = new();

        [JsonProperty("index")]
        public int Index { get; set; }
    }

    public class EmbeddingUsage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    // GPT accuracy check models
    public class AccuracyCheckRequest
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }

    public class AccuracyCheckResponse
    {
        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("feedback")]
        public string Feedback { get; set; } = string.Empty;
    }

    // Cultural sensitivity check models
    public class CulturalCheckRequest
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string TargetCountry { get; set; } = string.Empty;
    }

    public class CulturalCheckResponse
    {
        [JsonProperty("risk")]
        public string Risk { get; set; } = "Safe";

        [JsonProperty("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonProperty("suggestion")]
        public string Suggestion { get; set; } = string.Empty;

        [JsonProperty("sensitiveWords")]
        public List<string> SensitiveWords { get; set; } = new();
    }
}