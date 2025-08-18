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
                var audioFileName = $"{sanitizedFileName}.wav"; // Changed to WAV for better timing precision
                var audioFilePath = Path.Combine(outputPath, audioFileName);

                _logger.LogInformation($"Extracting audio from {videoFilePath} to {audioFilePath}");

                // Delete existing audio file if it exists
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                    _logger.LogInformation($"Deleted existing audio file: {audioFilePath}");
                }

                // Get video metadata to preserve original timing
                var videoInfo = await GetVideoMetadata(videoFilePath);
                _logger.LogInformation($"Video metadata: Duration={videoInfo.Duration}, Sample Rate={videoInfo.AudioSampleRate}, Channels={videoInfo.AudioChannels}");

                // Use absolute paths
                var absoluteVideoPath = Path.GetFullPath(videoFilePath);
                var absoluteAudioPath = Path.GetFullPath(audioFilePath);

                // Escape paths properly for FFmpeg (use 8.3 format for problematic paths)
                var escapedVideoPath = GetShortPath(absoluteVideoPath) ?? absoluteVideoPath;
                var escapedAudioPath = GetShortPath(absoluteAudioPath) ?? absoluteAudioPath;

                // FIXED: Improved FFmpeg command that preserves timing and uses appropriate sample rate
                // Key changes:
                // 1. Use -map 0:a:0 to select the first audio stream explicitly
                // 2. Use -af aresample=async=1 to handle timing issues
                // 3. Use appropriate sample rate (22050 Hz is good for speech recognition)
                // 4. Add -avoid_negative_ts make_zero to ensure consistent timestamps
                // 5. Use PCM WAV format for better precision
                var arguments = $"-y -hide_banner -loglevel error -i \"{escapedVideoPath}\" " +
                               $"-map 0:a:0 -vn " +
                               $"-acodec pcm_s16le " +
                               $"-ar 22050 " +
                               $"-ac 1 " +
                               $"-af \"aresample=async=1:first_pts=0\" " +
                               $"-avoid_negative_ts make_zero " +
                               $"-fflags +genpts " +
                               $"\"{escapedAudioPath}\"";

                _logger.LogInformation($"Running FFmpeg with improved timing preservation:");
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

                // Add timeout (5 minutes max for audio extraction)
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var processTask = process.WaitForExitAsync();

                var completedTask = await Task.WhenAny(processTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("FFmpeg process timed out after 5 minutes");
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

                // Verify timing integrity
                var extractedAudioInfo = await GetAudioMetadata(audioFilePath);
                var timingDiff = Math.Abs(videoInfo.Duration - extractedAudioInfo.Duration);

                if (timingDiff > 1.0) // More than 1 second difference is concerning
                {
                    _logger.LogWarning($"Timing discrepancy detected: Video={videoInfo.Duration:F2}s, Audio={extractedAudioInfo.Duration:F2}s, Diff={timingDiff:F2}s");
                }
                else
                {
                    _logger.LogInformation($"Timing verification passed: Video={videoInfo.Duration:F2}s, Audio={extractedAudioInfo.Duration:F2}s");
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

        // NEW: Method to get video metadata for timing verification
        private async Task<(double Duration, int AudioSampleRate, int AudioChannels)> GetVideoMetadata(string videoFilePath)
        {
            try
            {
                var escapedPath = GetShortPath(Path.GetFullPath(videoFilePath)) ?? Path.GetFullPath(videoFilePath);
                var arguments = $"-v quiet -show_entries format=duration:stream=sample_rate,channels -of csv=p=0 \"{escapedPath}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath.Replace("ffmpeg", "ffprobe"), // Use ffprobe for metadata
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start ffprobe process");

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                double duration = 0;
                int sampleRate = 22050; // Default
                int channels = 1; // Default

                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 1 && double.TryParse(parts[0], out var dur) && dur > 0)
                    {
                        duration = dur;
                    }
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var sr) && sr > 0)
                    {
                        sampleRate = sr;
                    }
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var ch) && ch > 0)
                    {
                        channels = ch;
                    }
                }

                return (duration, sampleRate, channels);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get video metadata, using defaults");
                return (0, 22050, 1);
            }
        }

        // NEW: Method to get audio metadata for verification
        private async Task<(double Duration, int SampleRate, int Channels)> GetAudioMetadata(string audioFilePath)
        {
            try
            {
                var escapedPath = GetShortPath(Path.GetFullPath(audioFilePath)) ?? Path.GetFullPath(audioFilePath);
                var arguments = $"-v quiet -show_entries format=duration:stream=sample_rate,channels -of csv=p=0 \"{escapedPath}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath.Replace("ffmpeg", "ffprobe"),
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return (0, 22050, 1);

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                double duration = 0;
                int sampleRate = 22050;
                int channels = 1;

                if (lines.Length > 0)
                {
                    var parts = lines[0].Split(',');
                    if (parts.Length >= 1 && double.TryParse(parts[0], out var dur))
                        duration = dur;
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var sr))
                        sampleRate = sr;
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var ch))
                        channels = ch;
                }

                return (duration, sampleRate, channels);
            }
            catch
            {
                return (0, 22050, 1);
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
            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { ' ', '(', ')', '[', ']', '{', '}', '&', '#', '%' })
                .ToArray();

            var sanitized = fileName;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Remove consecutive underscores and trim
            sanitized = Regex.Replace(sanitized, "_+", "_").Trim('_');

            // Ensure it's not empty
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "audio_file";

            return sanitized;
        }

        private string? GetShortPath(string path)
        {
            try
            {
                // Only use short path on Windows if the path is very long or contains problematic characters
                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    (path.Length > 200 || path.Any(c => char.IsHighSurrogate(c) || char.IsLowSurrogate(c))))
                {
                    const int MAX_PATH = 260;
                    var shortPath = new char[MAX_PATH];

                    if (GetShortPathNameW(path, shortPath, MAX_PATH) > 0)
                    {
                        return new string(shortPath).TrimEnd('\0');
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetShortPathNameW(string lpszLongPath, char[] lpszShortPath, int cchBuffer);
    }
}