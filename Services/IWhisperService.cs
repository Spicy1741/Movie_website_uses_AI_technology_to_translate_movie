using Film_website.Models;

namespace Film_website.Services
{
    public interface IWhisperService
    {
        Task<TranscriptionResponse> TranscribeAudioAsync(string audioFilePath, string language = "auto");
        Task<string> GenerateSrtAsync(string audioFilePath, string language = "auto");
    }
}