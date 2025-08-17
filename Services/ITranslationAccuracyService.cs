using Film_website.Models;

namespace Film_website.Services
{
    public interface ITranslationAccuracyService
    {
        Task<TranslationAccuracyResult> CheckTranslationAccuracyAsync(
            string originalFilePath,
            string translatedFilePath,
            string sourceLanguage,
            string targetLanguage,
            string targetCountry = "");

        Task<double> CalculateSemanticSimilarityAsync(string originalText, string translatedText);
        Task<AccuracyCheckResponse> CheckAccuracyWithGptAsync(AccuracyCheckRequest request);
        Task<CulturalCheckResponse> CheckCulturalSensitivityAsync(CulturalCheckRequest request);
        Task<List<string>> ReadSubtitleFileAsync(string filePath);
    }
}