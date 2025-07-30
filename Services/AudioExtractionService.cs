using System.Diagnostics;

namespace Film_website.Services
{
    public class AudioExtractionService : IAudioExtractionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AudioExtractionService> _logger;
        private readonly string _ffmpegPath;

        public AudioExtractionService(IConfiguration configuration, ILogger<AudioExtractionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _ffmpegPath = _configuration["FFmpeg:Path"] ?? "ffmpeg";
        }

        public async Task<string> ExtractAudioAsync(string videoFilePath, string outputPath)
        {
            try
            {
                if (!File.Exists(videoFilePath))
                    throw new FileNotFoundException($"Video file not found: {videoFilePath}");

                var audioFileName = Path.GetFileNameWithoutExtension(videoFilePath) + ".mp3";
                var audioFilePath = Path.Combine(outputPath, audioFileName);

                _logger.LogInformation($"Extracting audio from {videoFilePath} to {audioFilePath}");

                // Delete existing audio file if it exists
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                    _logger.LogInformation($"Deleted existing audio file: {audioFilePath}");
                }

                // Use absolute paths and add timeout
                var absoluteVideoPath = Path.GetFullPath(videoFilePath);
                var absoluteAudioPath = Path.GetFullPath(audioFilePath);

                // Simplified FFmpeg command for better compatibility
                var arguments = $"-y -i \"{absoluteVideoPath}\" -vn -acodec libmp3lame -ab 128k -ar 22050 \"{absoluteAudioPath}\"";

                _logger.LogInformation($"Running FFmpeg: {_ffmpegPath} {arguments}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(absoluteVideoPath)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start FFmpeg process");

                // Add timeout (5 minutes max)
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var processTask = process.WaitForExitAsync();

                var completedTask = await Task.WhenAny(processTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("FFmpeg process timed out after 5 minutes");
                    process.Kill(true);
                    throw new TimeoutException("FFmpeg process timed out");
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                _logger.LogInformation($"FFmpeg output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning($"FFmpeg stderr: {error}");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {error}");
                }

                if (!File.Exists(audioFilePath))
                {
                    throw new FileNotFoundException($"Audio extraction failed - output file not created: {audioFilePath}");
                }

                var audioFileInfo = new FileInfo(audioFilePath);
                if (audioFileInfo.Length == 0)
                {
                    throw new InvalidOperationException("Audio extraction failed - output file is empty");
                }

                _logger.LogInformation($"Audio extraction completed successfully: {audioFilePath} ({audioFileInfo.Length} bytes)");
                return audioFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting audio from video");
                throw;
            }
        }

        public bool IsVideoFile(string filePath)
        {
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm" };
            var extension = Path.GetExtension(filePath).ToLower();
            return videoExtensions.Contains(extension);
        }
    }
}