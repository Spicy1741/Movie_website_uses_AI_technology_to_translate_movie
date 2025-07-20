// Services/WhisperService.cs
using Film_website.Models;
using System.Diagnostics;

namespace Film_website.Services
{
    public class WhisperService : IWhisperService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhisperService> _logger;
        private readonly string _whisperPath;
        private readonly string _modelPath;

        public WhisperService(IConfiguration configuration, ILogger<WhisperService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _whisperPath = _configuration["WhisperSettings:ExecutablePath"] ?? "Whisper/WhisperDesktop.exe";
            _modelPath = _configuration["WhisperSettings:ModelPath"] ?? "Whisper/ggml-large-v1.bin";
        }

        public async Task<TranslationResponse> TranscribeAudioAsync(string audioFilePath, string language = "en")
        {
            var response = new TranslationResponse();
            var startTime = DateTime.Now;

            try
            {
                var outputPath = Path.ChangeExtension(audioFilePath, ".srt");

                var processInfo = new ProcessStartInfo
                {
                    FileName = _whisperPath,
                    Arguments = $"-m \"{_modelPath}\" -f \"{audioFilePath}\" -l {language} -osrt",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_whisperPath)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    response.Success = false;
                    response.Message = "Failed to start Whisper process";
                    return response;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && System.IO.File.Exists(outputPath))
                {
                    response.Success = true;
                    response.OriginalText = await System.IO.File.ReadAllTextAsync(outputPath);
                    response.Message = "Transcription completed successfully";
                    response.FileName = Path.GetFileName(outputPath);
                }
                else
                {
                    response.Success = false;
                    response.Message = $"Whisper process failed: {error}";
                    response.Errors.Add(error);
                }

                response.ProcessingTime = DateTime.Now - startTime;
                _logger.LogInformation($"Whisper transcription completed in {response.ProcessingTime.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error during transcription: {ex.Message}";
                response.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Error during Whisper transcription");
            }

            return response;
        }

        public async Task<string> ConvertVideoToAudioAsync(string videoFilePath)
        {
            // Simple implementation - in production, you'd use FFmpeg
            // For now, we'll assume Whisper can handle video files directly
            return videoFilePath;
        }

        public bool IsVideoFile(string filePath)
        {
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv" };
            var extension = Path.GetExtension(filePath).ToLower();
            return videoExtensions.Contains(extension);
        }

        public bool IsAudioFile(string filePath)
        {
            var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".flac" };
            var extension = Path.GetExtension(filePath).ToLower();
            return audioExtensions.Contains(extension);
        }
    }
}