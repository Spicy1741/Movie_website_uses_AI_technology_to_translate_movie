using Film_website.Models;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

namespace Film_website.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TranslationController : Controller
    {
        private readonly IWhisperService _whisperService;
        private readonly IGptTranslationService _translationService;
        private readonly IAudioExtractionService _audioExtractionService;
        private readonly ISrtGeneratorService _srtGeneratorService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            IWhisperService whisperService,
            IGptTranslationService translationService,
            IAudioExtractionService audioExtractionService,
            ISrtGeneratorService srtGeneratorService,
            IConfiguration configuration,
            ILogger<TranslationController> logger)
        {
            _whisperService = whisperService;
            _translationService = translationService;
            _audioExtractionService = audioExtractionService;
            _srtGeneratorService = srtGeneratorService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            ViewData["Title"] = "AI Video Translation - Film Website Admin";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessVideo(IFormFile videoFile, string targetLanguage = "Vietnamese")
        {
            string videoFilePath = null;
            string audioFilePath = null;

            try
            {
                _logger.LogInformation("=== STARTING VIDEO PROCESSING ===");
                _logger.LogInformation($"File: {videoFile?.FileName}, Size: {videoFile?.Length}");
                _logger.LogInformation($"Target Language: {targetLanguage}");

                // Validation checks
                if (videoFile == null || videoFile.Length == 0)
                {
                    ViewBag.Error = "Please select a video file to upload.";
                    return View("Upload");
                }

                // Check FFmpeg
                if (!await CheckFFmpegAsync())
                {
                    ViewBag.Error = "FFmpeg is not available. Please install FFmpeg and ensure it's accessible.";
                    return View("Upload");
                }

                // Check OpenAI
                if (!ValidateOpenAIConfig())
                {
                    ViewBag.Error = "OpenAI API is not properly configured.";
                    return View("Upload");
                }

                // File size validation
                var maxFileSizeMB = _configuration.GetValue<int>("FileSettings:MaxFileSizeMB", 500);
                var maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
                if (videoFile.Length > maxFileSizeBytes)
                {
                    ViewBag.Error = $"File size exceeds the maximum limit of {maxFileSizeMB}MB.";
                    return View("Upload");
                }

                // File type validation
                var allowedExtensions = _configuration.GetSection("FileSettings:AllowedVideoExtensions").Get<string[]>()
                    ?? new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
                var fileExtension = Path.GetExtension(videoFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ViewBag.Error = $"Invalid file type. Supported formats: {string.Join(", ", allowedExtensions)}";
                    return View("Upload");
                }

                // Setup paths
                var uploadPath = _configuration["FileSettings:UploadPath"] ?? "wwwroot/uploads";
                var downloadPath = _configuration["FileSettings:DownloadPath"] ?? "wwwroot/downloads";

                var fullUploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
                var fullDownloadPath = Path.Combine(Directory.GetCurrentDirectory(), downloadPath);

                Directory.CreateDirectory(fullUploadPath);
                Directory.CreateDirectory(fullDownloadPath);

                // Save uploaded file
                var originalFileName = Path.GetFileNameWithoutExtension(videoFile.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var uniqueFileName = $"{originalFileName}_{timestamp}{fileExtension}";
                videoFilePath = Path.Combine(fullUploadPath, uniqueFileName);

                using (var stream = new FileStream(videoFilePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                var fileInfo = new FileInfo(videoFilePath);
                _logger.LogInformation($"Video saved: {videoFilePath} ({fileInfo.Length} bytes)");

                // Extract audio
                _logger.LogInformation("Starting audio extraction...");
                audioFilePath = await _audioExtractionService.ExtractAudioAsync(videoFilePath, fullUploadPath);
                _logger.LogInformation($"Audio extracted: {audioFilePath}");

                // Transcribe audio
                _logger.LogInformation("Starting transcription...");
                var transcriptionResponse = await _whisperService.TranscribeAudioAsync(audioFilePath);

                if (!transcriptionResponse.Success || string.IsNullOrEmpty(transcriptionResponse.Text))
                {
                    _logger.LogError($"Transcription failed: {transcriptionResponse.Message}");
                    ViewBag.Error = $"Transcription failed: {transcriptionResponse.Message}";
                    return View("Upload");
                }

                _logger.LogInformation($"Transcription successful. Text length: {transcriptionResponse.Text.Length} characters");

                // Generate original SRT content
                var originalSrtContent = _srtGeneratorService.GenerateSrt(transcriptionResponse.Segments);

                // Translate if needed
                string translatedSrtContent = originalSrtContent;
                if (targetLanguage != "Original")
                {
                    _logger.LogInformation($"Starting translation to {targetLanguage}...");

                    var translatedSegments = new List<SrtEntry>();
                    foreach (var segment in transcriptionResponse.Segments)
                    {
                        var translatedText = await _translationService.TranslateTextAsync(segment.Text, targetLanguage);
                        translatedSegments.Add(new SrtEntry
                        {
                            Index = segment.Index,
                            StartTime = segment.StartTime,
                            EndTime = segment.EndTime,
                            Text = segment.Text,
                            TranslatedText = translatedText
                        });
                    }

                    translatedSrtContent = _srtGeneratorService.GenerateSrt(translatedSegments);
                    _logger.LogInformation("Translation completed successfully");
                }

                // Save SRT files
                var originalSrtFileName = $"{originalFileName}_original.srt";
                var translatedSrtFileName = $"{originalFileName}_{targetLanguage.ToLower()}.srt";

                var originalSrtPath = Path.Combine(fullDownloadPath, originalSrtFileName);
                var translatedSrtPath = Path.Combine(fullDownloadPath, translatedSrtFileName);

                await System.IO.File.WriteAllTextAsync(originalSrtPath, originalSrtContent, System.Text.Encoding.UTF8);
                await System.IO.File.WriteAllTextAsync(translatedSrtPath, translatedSrtContent, System.Text.Encoding.UTF8);

                _logger.LogInformation("=== SRT FILES SAVED SUCCESSFULLY ===");

                // Clean up temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                // Set ViewBag data for Result view
                ViewBag.OriginalSrtPath = $"/Translation/DownloadFile?fileName={originalSrtFileName}";
                ViewBag.TranslatedSrtPath = $"/Translation/DownloadFile?fileName={translatedSrtFileName}";
                ViewBag.TargetLanguage = targetLanguage;
                ViewBag.OriginalFileName = originalFileName;

                _logger.LogInformation("=== PROCESSING COMPLETED SUCCESSFULLY ===");
                return View("Result");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR PROCESSING VIDEO ===");

                // Clean up any temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                ViewBag.Error = $"An error occurred while processing your video: {ex.Message}";
                return View("Upload");
            }
        }

        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var downloadsPath = _configuration["FileSettings:DownloadPath"] ?? "wwwroot/downloads";
                var filePath = Path.Combine(downloadsPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found.");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = "application/octet-stream";

                // Fix: Use proper File method call
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return BadRequest("Error downloading file.");
            }
        }

        private void CleanupFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Cleaned up file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup file: {FilePath}", filePath);
            }
        }

        private async Task<bool> CheckFFmpegAsync()
        {
            try
            {
                var ffmpegPath = _configuration["FFmpeg:Path"] ?? "ffmpeg";

                var processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null) return false;

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var processTask = process.WaitForExitAsync();

                var completedTask = await Task.WhenAny(processTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    process.Kill(true);
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                _logger.LogInformation($"FFmpeg version check: {output.Substring(0, Math.Min(200, output.Length))}");

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg check failed");
                return false;
            }
        }

        private bool ValidateOpenAIConfig()
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("OpenAI API key is not configured");
                return false;
            }

            if (apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                _logger.LogError("OpenAI API key is not set - still using placeholder value");
                return false;
            }

            _logger.LogInformation("OpenAI API key is configured");
            return true;
        }
    }
}