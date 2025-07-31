namespace Film_website.Services
{
    public interface IAudioExtractionService
    {
        Task<string> ExtractAudioAsync(string videoFilePath, string outputPath);
        bool IsVideoFile(string filePath);
    }
}