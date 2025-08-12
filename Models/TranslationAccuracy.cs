using System.ComponentModel.DataAnnotations;

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

    // API request/response models for OpenAI
    public class OpenAIEmbeddingRequest
    {
        public string Model { get; set; } = "text-embedding-3-large";
        public List<string> Input { get; set; } = new();
        public string Encoding_format { get; set; } = "float";
    }

    public class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
        public EmbeddingUsage Usage { get; set; } = new();
    }

    public class EmbeddingData
    {
        public List<double> Embedding { get; set; } = new();
        public int Index { get; set; }
    }

    public class EmbeddingUsage
    {
        public int Prompt_tokens { get; set; }
        public int Total_tokens { get; set; }
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
        public int Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    // Cultural check models
    public class CulturalCheckRequest
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string TargetCountry { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }

    public class CulturalCheckResponse
    {
        public string Risk { get; set; } = string.Empty; // "Safe", "Potentially sensitive", "Inappropriate"
        public string Comment { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public List<string> SensitiveWords { get; set; } = new();
    }
}