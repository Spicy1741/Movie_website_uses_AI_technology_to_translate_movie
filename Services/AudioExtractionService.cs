using System.Diagnostics;
using System.Text.RegularExpressions;

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

                // Sanitize filename to avoid FFmpeg issues
                var originalFileName = Path.GetFileNameWithoutExtension(videoFilePath);
                var sanitizedFileName = SanitizeFileName(originalFileName);
                var audioFileName = $"{sanitizedFileName}.mp3";
                var audioFilePath = Path.Combine(outputPath, audioFileName);

                _logger.LogInformation($"Extracting audio from {videoFilePath} to {audioFilePath}");

                // Delete existing audio file if it exists
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                    _logger.LogInformation($"Deleted existing audio file: {audioFilePath}");
                }

                // Use absolute paths
                var absoluteVideoPath = Path.GetFullPath(videoFilePath);
                var absoluteAudioPath = Path.GetFullPath(audioFilePath);

                // Escape paths properly for FFmpeg (use 8.3 format for problematic paths)
                var escapedVideoPath = GetShortPath(absoluteVideoPath) ?? absoluteVideoPath;
                var escapedAudioPath = GetShortPath(absoluteAudioPath) ?? absoluteAudioPath;

                // Simplified FFmpeg command with better error handling
                var arguments = $"-y -hide_banner -loglevel error -i \"{escapedVideoPath}\" -vn -acodec libmp3lame -ab 128k -ar 16000 -ac 1 \"{escapedAudioPath}\"";

                _logger.LogInformation($"Running FFmpeg with escaped paths:");
                _logger.LogInformation($"Input: {escapedVideoPath}");
                _logger.LogInformation($"Output: {escapedAudioPath}");
                _logger.LogInformation($"Command: {_ffmpegPath} {arguments}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start FFmpeg process");

                // Create tasks for reading output and error streams
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Add timeout (3 minutes max for audio extraction)
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(3));
                var processTask = process.WaitForExitAsync();

                var completedTask = await Task.WhenAny(processTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("FFmpeg process timed out after 3 minutes");
                    try
                    {
                        process.Kill(true);
                    }
                    catch { }
                    throw new TimeoutException("FFmpeg process timed out during audio extraction");
                }

                // Get output and error messages
                var output = await outputTask;
                var error = await errorTask;

                _logger.LogInformation($"FFmpeg completed with exit code: {process.ExitCode}");

                if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogInformation($"FFmpeg output: {output}");
                }

                if (!string.IsNullOrEmpty(error))
                {
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation($"FFmpeg info: {error}");
                    }
                    else
                    {
                        _logger.LogError($"FFmpeg error: {error}");
                    }
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}. Error: {error}");
                }

                // Verify output file exists and has content
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

        private string SanitizeFileName(string fileName)
        {
            // Remove or replace problematic characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;

            // Replace invalid characters with underscores
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Replace other problematic characters
            sanitized = sanitized.Replace(":", "_")
                                 .Replace(";", "_")
                                 .Replace(",", "_")
                                 .Replace("'", "_")
                                 .Replace("\"", "_")
                                 .Replace("(", "_")
                                 .Replace(")", "_")
                                 .Replace("[", "_")
                                 .Replace("]", "_")
                                 .Replace("{", "_")
                                 .Replace("}", "_");

            // Remove multiple consecutive underscores
            sanitized = Regex.Replace(sanitized, "_+", "_");

            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            // Ensure it's not empty
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "audio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }

            // Limit length to avoid path issues
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }

            return sanitized;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetShortPathName(
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            string path,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            System.Text.StringBuilder shortPath,
            int shortPathLength);

        private string GetShortPath(string path)
        {
            try
            {
                var shortPath = new System.Text.StringBuilder(255);
                int result = GetShortPathName(path, shortPath, shortPath.Capacity);

                if (result != 0 && shortPath.Length > 0)
                {
                    return shortPath.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get short path for: {Path}", path);
            }

            return path; // Return original path if conversion fails
        }
    }
}