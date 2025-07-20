// Services/IWhisperService.cs
using Film_website.Models;

namespace Film_website.Services
{
    public interface IWhisperService
    {
        Task<TranslationResponse> TranscribeAudioAsync(string audioFilePath, string language = "en");
        Task<string> ConvertVideoToAudioAsync(string videoFilePath);
        bool IsVideoFile(string filePath);
        bool IsAudioFile(string filePath);
    }
}