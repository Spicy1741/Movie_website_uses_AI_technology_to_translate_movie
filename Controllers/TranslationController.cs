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
        public async Task<IActionResult> ProcessVideo(IFormFile videoFile, string targetLanguage = "Vietnamese", string sourceLanguage = "auto")
        {
            string videoFilePath = null;
            string audioFilePath = null;

            try
            {
                _logger.LogInformation("=== STARTING VIDEO PROCESSING ===");
                _logger.LogInformation($"File: {videoFile?.FileName}, Size: {videoFile?.Length}");
                _logger.LogInformation($"Source Language: {sourceLanguage}, Target Language: {targetLanguage}");

                // Basic validation with detailed logging
                if (videoFile == null)
                {
                    _logger.LogError("VideoFile is null");
                    ViewBag.Error = "No file was uploaded. Please select a video file.";
                    return View("Upload");
                }

                if (videoFile.Length == 0)
                {
                    _logger.LogError("VideoFile length is 0");
                    ViewBag.Error = "The uploaded file is empty. Please select a valid video file.";
                    return View("Upload");
                }

                if (string.IsNullOrEmpty(videoFile.FileName))
                {
                    _logger.LogError("VideoFile.FileName is null or empty");
                    ViewBag.Error = "The uploaded file has no name. Please try uploading again.";
                    return View("Upload");
                }

                _logger.LogInformation($"Original filename: '{videoFile.FileName}'");

                // Check FFmpeg availability
                if (!await CheckFFmpegAsync())
                {
                    ViewBag.Error = "FFmpeg is not installed or not accessible. Please install FFmpeg and ensure it's in your system PATH.";
                    return View("Upload");
                }

                // Check OpenAI configuration
                if (!ValidateOpenAIConfig())
                {
                    ViewBag.Error = "OpenAI API key is not properly configured. Please check your appsettings.json file.";
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

                // File type validation with null checks
                var fileExtension = Path.GetExtension(videoFile.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    _logger.LogError("File extension is null or empty");
                    ViewBag.Error = "Unable to determine file type. Please ensure your file has a proper extension.";
                    return View("Upload");
                }

                fileExtension = fileExtension.ToLower();
                _logger.LogInformation($"File extension: '{fileExtension}'");

                var allowedExtensions = _configuration.GetSection("FileSettings:AllowedVideoExtensions").Get<string[]>()
                    ?? new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ViewBag.Error = $"Invalid file type '{fileExtension}'. Supported formats: {string.Join(", ", allowedExtensions)}";
                    return View("Upload");
                }

                // Setup paths with detailed logging
                var uploadPath = _configuration["FileSettings:UploadPath"] ?? "wwwroot/uploads";
                var downloadPath = _configuration["FileSettings:DownloadPath"] ?? "wwwroot/downloads";

                _logger.LogInformation($"Upload path config: '{uploadPath}'");
                _logger.LogInformation($"Download path config: '{downloadPath}'");

                var currentDirectory = Directory.GetCurrentDirectory();
                _logger.LogInformation($"Current directory: '{currentDirectory}'");

                if (string.IsNullOrEmpty(currentDirectory))
                {
                    _logger.LogError("Current directory is null or empty");
                    ViewBag.Error = "Unable to determine application directory. Please contact support.";
                    return View("Upload");
                }

                var fullUploadPath = Path.Combine(currentDirectory, uploadPath);
                var fullDownloadPath = Path.Combine(currentDirectory, downloadPath);

                _logger.LogInformation($"Full upload path: '{fullUploadPath}'");
                _logger.LogInformation($"Full download path: '{fullDownloadPath}'");

                // Ensure directories exist
                try
                {
                    Directory.CreateDirectory(fullUploadPath);
                    Directory.CreateDirectory(fullDownloadPath);
                    _logger.LogInformation("Directories created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directories");
                    ViewBag.Error = "Failed to create required directories. Please check permissions.";
                    return View("Upload");
                }

                // Safe filename handling
                var originalFileName = Path.GetFileNameWithoutExtension(videoFile.FileName);
                if (string.IsNullOrEmpty(originalFileName))
                {
                    _logger.LogWarning("Original filename without extension is null or empty, using fallback");
                    originalFileName = "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                _logger.LogInformation($"Original filename without extension: '{originalFileName}'");

                var sanitizedFileName = SanitizeFileName(originalFileName);
                if (string.IsNullOrEmpty(sanitizedFileName))
                {
                    _logger.LogWarning("Sanitized filename is null or empty, using fallback");
                    sanitizedFileName = "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var uniqueFileName = $"{sanitizedFileName}_{timestamp}{fileExtension}";

                _logger.LogInformation($"Sanitized filename: '{sanitizedFileName}'");
                _logger.LogInformation($"Final filename: '{uniqueFileName}'");

                // Build video file path safely
                if (string.IsNullOrEmpty(fullUploadPath) || string.IsNullOrEmpty(uniqueFileName))
                {
                    _logger.LogError($"Path components are null: fullUploadPath='{fullUploadPath}', uniqueFileName='{uniqueFileName}'");
                    ViewBag.Error = "Failed to create file path. Please try again.";
                    return View("Upload");
                }

                videoFilePath = Path.Combine(fullUploadPath, uniqueFileName);
                _logger.LogInformation($"Video file path: '{videoFilePath}'");

                // Save uploaded file
                _logger.LogInformation("Starting file save operation");

                try
                {
                    using (var stream = new FileStream(videoFilePath, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(stream);
                    }
                    _logger.LogInformation("File saved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save uploaded file");
                    ViewBag.Error = "Failed to save uploaded file. Please try again.";
                    return View("Upload");
                }

                // Verify file was saved correctly
                var fileInfo = new FileInfo(videoFilePath);
                if (!fileInfo.Exists)
                {
                    _logger.LogError($"File does not exist after save: '{videoFilePath}'");
                    ViewBag.Error = "File was not saved correctly. Please try again.";
                    return View("Upload");
                }

                if (fileInfo.Length == 0)
                {
                    _logger.LogError("Saved file has zero length");
                    ViewBag.Error = "Saved file is empty. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation($"Video saved successfully: {videoFilePath} ({fileInfo.Length} bytes)");

                // Extract audio with null checks
                _logger.LogInformation("Starting audio extraction...");

                if (string.IsNullOrEmpty(videoFilePath))
                {
                    _logger.LogError("VideoFilePath is null before audio extraction");
                    ViewBag.Error = "Internal error: video file path is missing.";
                    return View("Upload");
                }

                if (string.IsNullOrEmpty(fullUploadPath))
                {
                    _logger.LogError("FullUploadPath is null before audio extraction");
                    ViewBag.Error = "Internal error: upload path is missing.";
                    return View("Upload");
                }

                try
                {
                    audioFilePath = await _audioExtractionService.ExtractAudioAsync(videoFilePath, fullUploadPath);
                    _logger.LogInformation($"Audio extraction completed: '{audioFilePath}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Audio extraction failed");
                    ViewBag.Error = $"Audio extraction failed: {ex.Message}";
                    return View("Upload");
                }

                // Verify audio file
                if (string.IsNullOrEmpty(audioFilePath))
                {
                    _logger.LogError("AudioFilePath is null after extraction");
                    ViewBag.Error = "Audio extraction returned null path.";
                    return View("Upload");
                }

                var audioFileInfo = new FileInfo(audioFilePath);
                if (!audioFileInfo.Exists || audioFileInfo.Length == 0)
                {
                    _logger.LogError($"Audio file is missing or empty: '{audioFilePath}'");
                    ViewBag.Error = "Audio extraction failed - no audio file created.";
                    return View("Upload");
                }

                _logger.LogInformation($"Audio extracted successfully: {audioFilePath} ({audioFileInfo.Length} bytes)");

                // Transcribe audio with explicit language handling
                _logger.LogInformation($"Starting transcription with source language: {sourceLanguage}...");

                TranscriptionResponse transcriptionResponse;
                try
                {
                    transcriptionResponse = await _whisperService.TranscribeAudioAsync(audioFilePath, sourceLanguage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transcription service failed");
                    ViewBag.Error = $"Transcription failed: {ex.Message}";
                    return View("Upload");
                }

                if (transcriptionResponse == null)
                {
                    _logger.LogError("Transcription response is null");
                    ViewBag.Error = "Transcription service returned no response.";
                    return View("Upload");
                }

                if (!transcriptionResponse.Success)
                {
                    _logger.LogError($"Transcription failed: {transcriptionResponse.Message}");
                    ViewBag.Error = $"Transcription failed: {transcriptionResponse.Message}";
                    return View("Upload");
                }

                if (string.IsNullOrEmpty(transcriptionResponse.Text))
                {
                    _logger.LogError("Transcription returned empty text");
                    ViewBag.Error = "No speech was detected in the audio. Please ensure your video contains clear speech.";
                    return View("Upload");
                }

                if (transcriptionResponse.Segments == null || !transcriptionResponse.Segments.Any())
                {
                    _logger.LogError("Transcription returned no segments");
                    ViewBag.Error = "Failed to create subtitle segments. Please try with a different video file.";
                    return View("Upload");
                }

                _logger.LogInformation($"Transcription successful. Text length: {transcriptionResponse.Text.Length} chars, Segments: {transcriptionResponse.Segments.Count}");

                // Generate original SRT content
                string originalSrtContent;
                try
                {
                    originalSrtContent = _srtGeneratorService.GenerateSrt(transcriptionResponse.Segments);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SRT generation failed");
                    ViewBag.Error = "Failed to generate subtitle file.";
                    return View("Upload");
                }

                if (string.IsNullOrEmpty(originalSrtContent))
                {
                    _logger.LogError("SRT generation returned empty content");
                    ViewBag.Error = "Failed to generate subtitle content.";
                    return View("Upload");
                }

                _logger.LogInformation($"Original SRT generated successfully. Length: {originalSrtContent.Length} characters");

                // Translation logic (rest of the method remains the same but with similar null checks)
                string translatedSrtContent = originalSrtContent;
                if (targetLanguage != "Original")
                {
                    _logger.LogInformation($"Starting translation to {targetLanguage}...");

                    var translatedSegments = new List<SrtEntry>();

                    foreach (var segment in transcriptionResponse.Segments)
                    {
                        try
                        {
                            if (segment?.Text != null)
                            {
                                var translatedText = await _translationService.TranslateTextAsync(segment.Text, targetLanguage);

                                translatedSegments.Add(new SrtEntry
                                {
                                    Index = segment.Index,
                                    StartTime = segment.StartTime,
                                    EndTime = segment.EndTime,
                                    Text = segment.Text,
                                    TranslatedText = translatedText ?? segment.Text
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to translate segment {segment?.Index}, using original text");
                            translatedSegments.Add(new SrtEntry
                            {
                                Index = segment?.Index ?? 0,
                                StartTime = segment?.StartTime ?? TimeSpan.Zero,
                                EndTime = segment?.EndTime ?? TimeSpan.Zero,
                                Text = segment?.Text ?? "",
                                TranslatedText = segment?.Text ?? ""
                            });
                        }
                    }

                    translatedSrtContent = _srtGeneratorService.GenerateSrt(translatedSegments);

                    if (string.IsNullOrEmpty(translatedSrtContent))
                    {
                        _logger.LogWarning("Translation SRT generation failed, using original");
                        translatedSrtContent = originalSrtContent;
                    }

                    _logger.LogInformation("Translation completed successfully");
                }

                // Save SRT files with null checks
                var originalSrtFileName = $"{sanitizedFileName}_original.srt";
                var translatedSrtFileName = targetLanguage == "Original"
                    ? $"{sanitizedFileName}_original.srt"
                    : $"{sanitizedFileName}_{targetLanguage.ToLower().Replace(" ", "_")}.srt";

                if (string.IsNullOrEmpty(fullDownloadPath))
                {
                    _logger.LogError("FullDownloadPath is null when saving SRT files");
                    ViewBag.Error = "Internal error: download path is missing.";
                    return View("Upload");
                }

                var originalSrtPath = Path.Combine(fullDownloadPath, originalSrtFileName);
                var translatedSrtPath = Path.Combine(fullDownloadPath, translatedSrtFileName);

                _logger.LogInformation($"Saving SRT files to: '{originalSrtPath}' and '{translatedSrtPath}'");

                try
                {
                    await System.IO.File.WriteAllTextAsync(originalSrtPath, originalSrtContent, System.Text.Encoding.UTF8);
                    await System.IO.File.WriteAllTextAsync(translatedSrtPath, translatedSrtContent, System.Text.Encoding.UTF8);
                    _logger.LogInformation("SRT files saved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save SRT files");
                    ViewBag.Error = "Failed to save subtitle files. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation("=== SRT FILES SAVED SUCCESSFULLY ===");

                // Clean up temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                // Set ViewBag data for Result view
                ViewBag.OriginalSrtPath = $"/Translation/DownloadFile?fileName={originalSrtFileName}";
                ViewBag.TranslatedSrtPath = $"/Translation/DownloadFile?fileName={translatedSrtFileName}";
                ViewBag.TargetLanguage = targetLanguage;
                ViewBag.OriginalFileName = sanitizedFileName;
                ViewBag.SourceLanguage = sourceLanguage;
                ViewBag.TranscriptionLength = transcriptionResponse.Text.Length;
                ViewBag.SegmentCount = transcriptionResponse.Segments.Count;

                _logger.LogInformation("=== PROCESSING COMPLETED SUCCESSFULLY ===");
                return View("Result");
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument error - Parameter: {ParamName}", ex.ParamName);
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);
                ViewBag.Error = $"Internal error: Missing required parameter ({ex.ParamName}). Please try again.";
                return View("Upload");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== UNEXPECTED ERROR PROCESSING VIDEO ===");
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);
                ViewBag.Error = $"An unexpected error occurred: {ex.Message}. Please try again.";
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

            if (!apiKey.StartsWith("sk-"))
            {
                _logger.LogError("OpenAI API key format is invalid - should start with 'sk-'");
                return false;
            }

            _logger.LogInformation("OpenAI API key is configured and appears valid");
            return true;
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Input filename is null or empty, using fallback");
                return "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }

            try
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
                                     .Replace("}", "_")
                                     .Replace(" ", "_");

                // Remove multiple consecutive underscores
                sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, "_+", "_");

                // Remove leading/trailing underscores
                sanitized = sanitized.Trim('_');

                // Ensure it's not empty after sanitization
                if (string.IsNullOrEmpty(sanitized))
                {
                    sanitized = "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                // Limit length to avoid path issues
                if (sanitized.Length > 100)
                {
                    sanitized = sanitized.Substring(0, 100).TrimEnd('_');
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing filename: {FileName}", fileName);
                return "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
        }
    }
}