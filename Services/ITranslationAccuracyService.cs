using Film_website.Models;

namespace Film_website.Services
{
    public interface ITranslationAccuracyService
    {
        /// <summary>
        /// Run the complete 3-layer pipeline to check translation accuracy
        /// </summary>
        /// <param name="originalFilePath">Path to the original subtitle file</param>
        /// <param name="translatedFilePath">Path to the translated subtitle file</param>
        /// <param name="sourceLanguage">Source language name</param>
        /// <param name="targetLanguage">Target language name</param>
        /// <param name="targetCountry">Target country for cultural context</param>
        /// <returns>Complete accuracy analysis result</returns>
        Task<TranslationAccuracyResult> CheckTranslationAccuracyAsync(
            string originalFilePath,
            string translatedFilePath,
            string sourceLanguage,
            string targetLanguage,
            string targetCountry = "");

        /// <summary>
        /// Layer 1: Calculate semantic similarity using embeddings
        /// </summary>
        Task<double> CalculateSemanticSimilarityAsync(string originalText, string translatedText);

        /// <summary>
        /// Layer 2: Get accuracy score and feedback from GPT
        /// </summary>
        Task<AccuracyCheckResponse> CheckAccuracyWithGptAsync(AccuracyCheckRequest request);

        /// <summary>
        /// Layer 3: Check for cultural and political sensitivity
        /// </summary>
        Task<CulturalCheckResponse> CheckCulturalSensitivityAsync(CulturalCheckRequest request);

        /// <summary>
        /// Read and parse subtitle file content
        /// </summary>
        Task<List<string>> ReadSubtitleFileAsync(string filePath);
    }
}