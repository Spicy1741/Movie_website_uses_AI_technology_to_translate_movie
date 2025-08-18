using Microsoft.AspNetCore.Mvc;
using Film_website.Services;
using Film_website.Models;
using System.Text.RegularExpressions;

namespace Film_website.Controllers
{
    public class TranslationController : Controller
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly IAudioExtractionService _audioExtractionService;
        private readonly IWhisperService _whisperService;
        private readonly IGptTranslationService _translationService;
        private readonly ISrtGeneratorService _srtGeneratorService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TranslationController(
            ILogger<TranslationController> logger,
            IAudioExtractionService audioExtractionService,
            IWhisperService whisperService,
            IGptTranslationService translationService,
            ISrtGeneratorService srtGeneratorService,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _audioExtractionService = audioExtractionService;
            _whisperService = whisperService;
            _translationService = translationService;
            _srtGeneratorService = srtGeneratorService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Upload()
        {
            return View();
        }

        // GET action to display results that can be safely refreshed
        [HttpGet]
        public IActionResult Result(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    TempData["Error"] = "Invalid session. Please process a new video.";
                    return RedirectToAction("Upload");
                }

                // Retrieve translation data from session
                var sessionKey = $"TranslationResult_{sessionId}";
                var sessionData = HttpContext.Session.GetString(sessionKey);

                if (string.IsNullOrEmpty(sessionData))
                {
                    TempData["Error"] = "Translation session expired. Please process a new video.";
                    return RedirectToAction("Upload");
                }

                var translationResult = System.Text.Json.JsonSerializer.Deserialize<TranslationSessionData>(sessionData);

                // Verify files still exist
                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                var originalFilePath = Path.Combine(downloadsPath, translationResult.OriginalSrtFileName);
                var translatedFilePath = Path.Combine(downloadsPath, translationResult.TranslatedSrtFileName);

                if (!System.IO.File.Exists(originalFilePath) || !System.IO.File.Exists(translatedFilePath))
                {
                    TempData["Error"] = "Translation files not found. They may have been deleted. Please process the video again.";
                    return RedirectToAction("Upload");
                }

                // Set ViewBag data for Result view
                ViewBag.OriginalSrtPath = $"/Translation/DownloadFile?fileName={translationResult.OriginalSrtFileName}";
                ViewBag.TranslatedSrtPath = $"/Translation/DownloadFile?fileName={translationResult.TranslatedSrtFileName}";
                ViewBag.OriginalSrtFileName = translationResult.OriginalSrtFileName;
                ViewBag.TranslatedSrtFileName = translationResult.TranslatedSrtFileName;
                ViewBag.TargetLanguage = translationResult.TargetLanguage;
                ViewBag.OriginalFileName = translationResult.OriginalFileName;
                ViewBag.SourceLanguage = translationResult.SourceLanguage;
                ViewBag.ProcessedDate = translationResult.ProcessingDate;
                ViewBag.TranscriptionLength = translationResult.TranscriptionLength;
                ViewBag.SegmentCount = translationResult.SegmentCount;

                return View(translationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying translation result");
                TempData["Error"] = "Error loading translation result. Please try processing again.";
                return RedirectToAction("Upload");
            }
        }

        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                var filePath = Path.Combine(downloadsPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found.");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = "application/octet-stream";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {fileName}");
                return BadRequest("Error downloading file.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessVideo(IFormFile videoFile, string targetLanguage = "Vietnamese", string sourceLanguage = "auto")
        {
            string videoFilePath = null;
            string audioFilePath = null;

            try
            {
                _logger.LogInformation("=== STARTING VIDEO PROCESSING WITH ENHANCED TIMESTAMP VALIDATION ===");
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

                if (videoFile.Length > 500 * 1024 * 1024) // 500MB limit
                {
                    _logger.LogError($"VideoFile too large: {videoFile.Length} bytes");
                    ViewBag.Error = "File size exceeds 500MB limit. Please select a smaller video file.";
                    return View("Upload");
                }

                // Valid video extensions
                var validExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
                var fileExtension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();

                if (!validExtensions.Contains(fileExtension))
                {
                    _logger.LogError($"Invalid file extension: {fileExtension}");
                    ViewBag.Error = $"Invalid file format. Supported formats: {string.Join(", ", validExtensions)}";
                    return View("Upload");
                }

                _logger.LogInformation("=== FILE VALIDATION PASSED ===");

                // Create upload directory
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation($"Created uploads directory: {uploadsPath}");
                }

                // Save uploaded video file
                var originalFileName = Path.GetFileNameWithoutExtension(videoFile.FileName);
                var sanitizedFileName = SanitizeFileName(originalFileName);
                var fileExtensionClean = Path.GetExtension(videoFile.FileName);
                var uniqueFileName = $"{sanitizedFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtensionClean}";
                videoFilePath = Path.Combine(uploadsPath, uniqueFileName);

                _logger.LogInformation($"Saving video file to: {videoFilePath}");

                using (var stream = new FileStream(videoFilePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                if (!System.IO.File.Exists(videoFilePath))
                {
                    _logger.LogError("Failed to save video file");
                    ViewBag.Error = "Failed to save uploaded video file. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation($"=== VIDEO FILE SAVED: {videoFilePath} ===");

                // Extract audio from video with enhanced timing preservation
                _logger.LogInformation("=== STARTING ENHANCED AUDIO EXTRACTION ===");
                audioFilePath = await _audioExtractionService.ExtractAudioAsync(videoFilePath, uploadsPath);

                if (string.IsNullOrEmpty(audioFilePath) || !System.IO.File.Exists(audioFilePath))
                {
                    _logger.LogError($"Audio extraction failed. Audio file path: {audioFilePath}");
                    CleanupFile(videoFilePath);
                    ViewBag.Error = "Failed to extract audio from video. Please ensure the video file is valid and try again.";
                    return View("Upload");
                }

                _logger.LogInformation($"=== AUDIO EXTRACTED: {audioFilePath} ===");

                // Transcribe audio using enhanced Whisper service
                _logger.LogInformation("=== STARTING ENHANCED TRANSCRIPTION ===");
                var transcriptionResponse = await _whisperService.TranscribeAudioAsync(audioFilePath, sourceLanguage);

                if (transcriptionResponse == null || !transcriptionResponse.Success || string.IsNullOrWhiteSpace(transcriptionResponse.Text))
                {
                    _logger.LogError($"Transcription failed: {transcriptionResponse?.Message ?? "Unknown error"}");
                    CleanupFile(videoFilePath);
                    CleanupFile(audioFilePath);
                    ViewBag.Error = $"Transcription failed: {transcriptionResponse?.Message ?? "Unknown error"}. The video might not contain clear speech or the audio quality might be too low.";
                    return View("Upload");
                }

                _logger.LogInformation($"=== TRANSCRIPTION COMPLETED: {transcriptionResponse.Text.Length} characters, {transcriptionResponse.Segments.Count} segments ===");

                // ENHANCED: Validate transcript timing before proceeding
                var timingValidation = ValidateTranscriptTiming(transcriptionResponse.Segments);
                if (!timingValidation.IsValid)
                {
                    _logger.LogWarning($"Timing validation issues found: {string.Join(", ", timingValidation.Issues)}");
                    // Continue processing but log the issues
                }

                // Generate SRT files with enhanced translation and timing validation
                _logger.LogInformation("=== GENERATING SRT FILES WITH ENHANCED TIMING ===");

                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                var originalSrtFileName = $"{sanitizedFileName}_original_{DateTime.Now:yyyyMMdd_HHmmss}.srt";
                var translatedSrtFileName = $"{sanitizedFileName}_{targetLanguage.ToLower()}_{DateTime.Now:yyyyMMdd_HHmmss}.srt";

                var originalSrtPath = Path.Combine(downloadsPath, originalSrtFileName);
                var translatedSrtPath = Path.Combine(downloadsPath, translatedSrtFileName);

                // Generate original SRT with enhanced validation
                var originalSrtContent = _srtGeneratorService.GenerateSrt(transcriptionResponse.Segments);
                await System.IO.File.WriteAllTextAsync(originalSrtPath, originalSrtContent);

                // Generate translated SRT by translating each segment individually with timing preservation
                var translatedSegments = new List<SrtEntry>();

                _logger.LogInformation("=== TRANSLATING INDIVIDUAL SEGMENTS WITH TIMING PRESERVATION ===");
                for (int i = 0; i < transcriptionResponse.Segments.Count; i++)
                {
                    var segment = transcriptionResponse.Segments[i];
                    string translatedSegmentText;

                    try
                    {
                        // FIXED: using correct method signature from IGptTranslationService
                        translatedSegmentText = await _translationService.TranslateTextAsync(
                            segment.Text,
                            targetLanguage,
                            sourceLanguage == "auto" ? "English" : sourceLanguage);

                        _logger.LogDebug($"Translated segment {i + 1}/{transcriptionResponse.Segments.Count}: '{segment.Text}' -> '{translatedSegmentText}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to translate segment {i + 1}, using original text");
                        translatedSegmentText = segment.Text; // Fallback to original text
                    }

                    // ENHANCED: Preserve exact timing from original segment
                    translatedSegments.Add(new SrtEntry
                    {
                        Index = segment.Index,
                        StartTime = segment.StartTime, // Preserve exact timing
                        EndTime = segment.EndTime,     // Preserve exact timing
                        Text = translatedSegmentText
                    });
                }

                // Generate translated SRT content with enhanced validation
                var translatedSrtContent = _srtGeneratorService.GenerateSrt(translatedSegments);
                await System.IO.File.WriteAllTextAsync(translatedSrtPath, translatedSrtContent);

                if (!System.IO.File.Exists(originalSrtPath) || !System.IO.File.Exists(translatedSrtPath))
                {
                    _logger.LogError($"Failed to create SRT files. Original: {System.IO.File.Exists(originalSrtPath)}, Translated: {System.IO.File.Exists(translatedSrtPath)}");
                    CleanupFile(videoFilePath);
                    CleanupFile(audioFilePath);
                    ViewBag.Error = "Failed to generate subtitle files. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation("=== SRT FILES SAVED SUCCESSFULLY WITH TIMING VALIDATION ===");

                // ENHANCED: Log final timing statistics
                LogTimingStatistics(transcriptionResponse.Segments, translatedSegments);

                // Clean up temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                // Store translation data in session
                var sessionId = Guid.NewGuid().ToString();
                var translationData = new TranslationSessionData
                {
                    SessionId = sessionId,
                    OriginalSrtFileName = originalSrtFileName,
                    TranslatedSrtFileName = translatedSrtFileName,
                    TargetLanguage = targetLanguage,
                    OriginalFileName = sanitizedFileName,
                    SourceLanguage = sourceLanguage == "auto" ? "English" : sourceLanguage,
                    ProcessingDate = DateTime.Now,
                    TranscriptionLength = transcriptionResponse.Text.Length,
                    SegmentCount = transcriptionResponse.Segments.Count
                };

                // Store in session with the correct key format
                var sessionKey = $"TranslationResult_{sessionId}";
                HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(translationData));

                _logger.LogInformation($"=== VIDEO PROCESSING COMPLETED SUCCESSFULLY, SESSION: {sessionId} ===");

                // Redirect to Result action instead of returning view directly
                return RedirectToAction("Result", new { sessionId = sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video");
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);
                ViewBag.Error = $"An error occurred while processing the video: {ex.Message}. Please try again.";
                return View("Upload");
            }
        }

        // ENHANCED: New method for comprehensive timing validation
        private (bool IsValid, List<string> Issues) ValidateTranscriptTiming(List<SrtEntry> segments)
        {
            var issues = new List<string>();

            if (segments == null || segments.Count == 0)
            {
                issues.Add("No segments to validate");
                return (false, issues);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                // Check for reasonable segment duration
                var duration = segment.EndTime - segment.StartTime;
                if (duration.TotalSeconds < 0.1)
                {
                    issues.Add($"Segment {i + 1}: Very short duration ({duration.TotalMilliseconds:F0}ms)");
                }
                else if (duration.TotalSeconds > 20)
                {
                    issues.Add($"Segment {i + 1}: Very long duration ({duration.TotalSeconds:F1}s)");
                }

                // Check for negative timestamps
                if (segment.StartTime < TimeSpan.Zero || segment.EndTime < TimeSpan.Zero)
                {
                    issues.Add($"Segment {i + 1}: Negative timestamp detected");
                }

                // Check for overlaps
                if (i > 0)
                {
                    var prevSegment = segments[i - 1];
                    if (segment.StartTime < prevSegment.EndTime)
                    {
                        var overlap = prevSegment.EndTime - segment.StartTime;
                        issues.Add($"Segment {i + 1}: Overlaps with previous segment by {overlap.TotalMilliseconds:F0}ms");
                    }
                }
            }

            return (issues.Count == 0, issues);
        }

        // ENHANCED: New method to log timing statistics
        private void LogTimingStatistics(List<SrtEntry> originalSegments, List<SrtEntry> translatedSegments)
        {
            if (originalSegments.Count > 0)
            {
                var totalDuration = originalSegments.Last().EndTime.TotalSeconds;
                var avgSegmentDuration = originalSegments.Average(s => (s.EndTime - s.StartTime).TotalSeconds);
                var minDuration = originalSegments.Min(s => (s.EndTime - s.StartTime).TotalSeconds);
                var maxDuration = originalSegments.Max(s => (s.EndTime - s.StartTime).TotalSeconds);

                _logger.LogInformation($"=== TIMING STATISTICS ===");
                _logger.LogInformation($"Total segments: {originalSegments.Count}");
                _logger.LogInformation($"Total duration: {totalDuration:F2} seconds");
                _logger.LogInformation($"Average segment duration: {avgSegmentDuration:F2} seconds");
                _logger.LogInformation($"Min segment duration: {minDuration:F2} seconds");
                _logger.LogInformation($"Max segment duration: {maxDuration:F2} seconds");

                // Check timing consistency between original and translated
                if (translatedSegments.Count == originalSegments.Count)
                {
                    var timingDifferences = new List<double>();
                    for (int i = 0; i < originalSegments.Count; i++)
                    {
                        var origDuration = (originalSegments[i].EndTime - originalSegments[i].StartTime).TotalSeconds;
                        var transDuration = (translatedSegments[i].EndTime - translatedSegments[i].StartTime).TotalSeconds;
                        timingDifferences.Add(Math.Abs(origDuration - transDuration));
                    }

                    var maxTimingDiff = timingDifferences.Max();
                    var avgTimingDiff = timingDifferences.Average();

                    _logger.LogInformation($"Max timing difference between original and translated: {maxTimingDiff:F3} seconds");
                    _logger.LogInformation($"Average timing difference: {avgTimingDiff:F3} seconds");

                    if (maxTimingDiff > 0.1)
                    {
                        _logger.LogWarning($"Significant timing differences detected between original and translated subtitles");
                    }
                }

                _logger.LogInformation($"=== END TIMING STATISTICS ===");
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
                _logger.LogWarning(ex, $"Failed to cleanup file: {filePath}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { ' ', '(', ')', '[', ']', '{', '}', '&', '#', '%' })
                .ToArray();

            var sanitized = fileName;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            sanitized = Regex.Replace(sanitized, "_+", "_").Trim('_');

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "video_file";

            return sanitized;
        }
    }

    // ENHANCED: Updated session data model  
    public class TranslationSessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public string OriginalSrtFileName { get; set; } = string.Empty;
        public string TranslatedSrtFileName { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public int TranscriptionLength { get; set; }
        public int SegmentCount { get; set; }
    }
}