using Film_website.Models;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Film_website.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TranslationController : Controller
    {
        private readonly IWhisperService _whisperService;
        private readonly IGptTranslationService _translationService;
        private readonly IAudioExtractionService _audioExtractionService;
        private readonly ISrtGeneratorService _srtGeneratorService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TranslationController> _logger;
        private readonly ITranslationAccuracyService _accuracyService;

        public TranslationController(
            IWhisperService whisperService,
            IGptTranslationService translationService,
            IAudioExtractionService audioExtractionService,
            ISrtGeneratorService srtGeneratorService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<TranslationController> logger,
            ITranslationAccuracyService accuracyService)
        {
            _whisperService = whisperService;
            _translationService = translationService;
            _audioExtractionService = audioExtractionService;
            _srtGeneratorService = srtGeneratorService;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
            _accuracyService = accuracyService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            ViewData["Title"] = "AI Video Translation - Film Website Admin";
            return View();
        }

        // *** NEW: GET action to display results that can be safely refreshed ***
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

                var translationResult = JsonConvert.DeserializeObject<TranslationSessionData>(sessionData);

                // Verify files still exist
                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                var originalFilePath = Path.Combine(downloadsPath, translationResult.OriginalSrtFileName);
                var translatedFilePath = Path.Combine(downloadsPath, translationResult.TranslatedSrtFileName);

                if (!System.IO.File.Exists(originalFilePath) || !System.IO.File.Exists(translatedFilePath))
                {
                    TempData["Error"] = "Translation files not found. They may have been deleted. Please process the video again.";
                    return RedirectToAction("Upload");
                }

                // Set ViewBag data for Result view (same as before)
                ViewBag.OriginalSrtPath = $"/Translation/DownloadFile?fileName={translationResult.OriginalSrtFileName}";
                ViewBag.TranslatedSrtPath = $"/Translation/DownloadFile?fileName={translationResult.TranslatedSrtFileName}";
                ViewBag.OriginalSrtFileName = translationResult.OriginalSrtFileName;
                ViewBag.TranslatedSrtFileName = translationResult.TranslatedSrtFileName;
                ViewBag.TargetLanguage = translationResult.TargetLanguage;
                ViewBag.OriginalFileName = translationResult.OriginalFileName;
                ViewBag.SourceLanguage = translationResult.SourceLanguage;
                ViewBag.TranscriptionLength = translationResult.TranscriptionLength;
                ViewBag.SegmentCount = translationResult.SegmentCount;
                ViewBag.ProcessedDate = translationResult.ProcessedDate;

                // *** NEW: Check if accuracy results exist and pass them to view ***
                if (translationResult.AccuracyResults != null)
                {
                    ViewBag.HasAccuracyResults = true;
                    ViewBag.AccuracyResults = JsonConvert.SerializeObject(translationResult.AccuracyResults);
                    ViewBag.AccuracyCheckDate = translationResult.AccuracyCheckDate;
                    ViewBag.TargetCountry = translationResult.TargetCountry;

                    _logger.LogInformation($"Restored accuracy results for session {sessionId} - Score: {translationResult.AccuracyResults.AccuracyScore}%");
                }
                else
                {
                    // *** NEW: Check for temporary accuracy results (fallback) ***
                    var lookupKey = $"AccuracyResults_{translationResult.OriginalSrtFileName}_{translationResult.TranslatedSrtFileName}";
                    var tempAccuracyData = HttpContext.Session.GetString(lookupKey);

                    if (!string.IsNullOrEmpty(tempAccuracyData))
                    {
                        try
                        {
                            var tempData = JsonConvert.DeserializeObject<dynamic>(tempAccuracyData);
                            ViewBag.HasAccuracyResults = true;
                            ViewBag.AccuracyResults = JsonConvert.SerializeObject(tempData.AccuracyResults);
                            ViewBag.AccuracyCheckDate = tempData.AccuracyCheckDate;
                            ViewBag.TargetCountry = tempData.TargetCountry;

                            _logger.LogInformation($"Restored accuracy results from temporary session for {sessionId}");

                            // Clean up temporary session
                            HttpContext.Session.Remove(lookupKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to restore temporary accuracy results");
                            ViewBag.HasAccuracyResults = false;
                        }
                    }
                    else
                    {
                        ViewBag.HasAccuracyResults = false;
                    }
                }

                ViewData["Title"] = "Translation Results - Film Website Admin";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying translation results for session {SessionId}", sessionId);
                TempData["Error"] = "Error displaying translation results. Please try again.";
                return RedirectToAction("Upload");
            }
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

                // Extract audio from video (*** FIXED: using correct method signature ***)
                _logger.LogInformation("=== STARTING AUDIO EXTRACTION ===");
                audioFilePath = await _audioExtractionService.ExtractAudioAsync(videoFilePath, uploadsPath);

                if (string.IsNullOrEmpty(audioFilePath) || !System.IO.File.Exists(audioFilePath))
                {
                    _logger.LogError($"Audio extraction failed. Audio file path: {audioFilePath}");
                    CleanupFile(videoFilePath);
                    ViewBag.Error = "Failed to extract audio from video. Please ensure the video file is valid and try again.";
                    return View("Upload");
                }

                _logger.LogInformation($"=== AUDIO EXTRACTED: {audioFilePath} ===");

                // Transcribe audio using Whisper (*** FIXED: using correct method name ***)
                _logger.LogInformation("=== STARTING TRANSCRIPTION ===");
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

                // Generate SRT files with translation
                _logger.LogInformation("=== GENERATING SRT FILES WITH TRANSLATION ===");

                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                var originalSrtFileName = $"{sanitizedFileName}_original_{DateTime.Now:yyyyMMdd_HHmmss}.srt";
                var translatedSrtFileName = $"{sanitizedFileName}_{targetLanguage.ToLower()}_{DateTime.Now:yyyyMMdd_HHmmss}.srt";

                var originalSrtPath = Path.Combine(downloadsPath, originalSrtFileName);
                var translatedSrtPath = Path.Combine(downloadsPath, translatedSrtFileName);

                // Generate original SRT
                var originalSrtContent = _srtGeneratorService.GenerateSrt(transcriptionResponse.Segments);
                await System.IO.File.WriteAllTextAsync(originalSrtPath, originalSrtContent);

                // Generate translated SRT by translating each segment individually
                var translatedSegments = new List<SrtEntry>();

                _logger.LogInformation("=== TRANSLATING INDIVIDUAL SEGMENTS ===");
                for (int i = 0; i < transcriptionResponse.Segments.Count; i++)
                {
                    var segment = transcriptionResponse.Segments[i];
                    string translatedSegmentText;

                    try
                    {
                        // *** FIXED: using correct method signature and parameter order ***
                        translatedSegmentText = await _translationService.TranslateTextAsync(
                            segment.Text,
                            targetLanguage,
                            sourceLanguage == "auto" ? "English" : sourceLanguage);

                        _logger.LogInformation($"Translated segment {i + 1}/{transcriptionResponse.Segments.Count}: '{segment.Text}' -> '{translatedSegmentText}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to translate segment {i + 1}, using original text");
                        translatedSegmentText = segment.Text; // Fallback to original text
                    }

                    translatedSegments.Add(new SrtEntry
                    {
                        Index = segment.Index,
                        StartTime = segment.StartTime,
                        EndTime = segment.EndTime,
                        Text = translatedSegmentText
                    });
                }

                // Generate translated SRT content
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

                _logger.LogInformation("=== SRT FILES SAVED SUCCESSFULLY ===");

                // Clean up temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                // *** NEW: Store translation data in session ***
                var sessionId = Guid.NewGuid().ToString();
                var translationData = new TranslationSessionData
                {
                    SessionId = sessionId,
                    OriginalSrtFileName = originalSrtFileName,
                    TranslatedSrtFileName = translatedSrtFileName,
                    TargetLanguage = targetLanguage,
                    OriginalFileName = sanitizedFileName,
                    SourceLanguage = sourceLanguage == "auto" ? "English" : sourceLanguage,
                    TranscriptionLength = transcriptionResponse.Text.Length,
                    SegmentCount = transcriptionResponse.Segments.Count,
                    ProcessedDate = DateTime.Now
                };

                var sessionKey = $"TranslationResult_{sessionId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(translationData));

                _logger.LogInformation($"=== PROCESSING COMPLETED SUCCESSFULLY, SESSION: {sessionId} ===");

                // *** NEW: Redirect to GET endpoint instead of returning view directly ***
                return RedirectToAction("Result", new { sessionId = sessionId });
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

        private void CleanupFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Cleaned up temporary file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to cleanup file: {filePath}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitized = fileName;

                foreach (char c in invalidChars)
                {
                    sanitized = sanitized.Replace(c, '_');
                }

                sanitized = sanitized.Replace(":", "_")
                                     .Replace(";", "_")
                                     .Replace(",", "_")
                                     .Replace(" ", "_")
                                     .Replace("(", "")
                                     .Replace(")", "");

                if (sanitized.Length > 50)
                {
                    sanitized = sanitized.Substring(0, 50);
                }

                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    sanitized = "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                _logger.LogInformation($"Sanitized filename: '{fileName}' -> '{sanitized}'");
                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sanitizing filename: {fileName}");
                return "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
        }

        // =========================================================
        // *** ACCURACY CHECK METHODS (unchanged) ***
        // =========================================================

        /// <summary>
        /// Check translation accuracy using 3-layer pipeline
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAccuracy(string originalFile, string translatedFile,
            string sourceLanguage, string targetLanguage, string targetCountry = "", string sessionId = "")
        {
            try
            {
                _logger.LogInformation($"Starting accuracy check for: {originalFile} -> {translatedFile}");

                // Validate file paths
                if (string.IsNullOrEmpty(originalFile) || string.IsNullOrEmpty(translatedFile))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Original file and translated file paths are required"
                    });
                }

                // Construct full file paths
                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");
                var originalFilePath = Path.Combine(downloadsPath, originalFile);
                var translatedFilePath = Path.Combine(downloadsPath, translatedFile);

                // Check if files exist
                if (!System.IO.File.Exists(originalFilePath))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Original file not found: {originalFile}"
                    });
                }

                if (!System.IO.File.Exists(translatedFilePath))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Translated file not found: {translatedFile}"
                    });
                }

                // Set default target country if not provided
                if (string.IsNullOrEmpty(targetCountry))
                {
                    targetCountry = GetDefaultCountryForLanguage(targetLanguage);
                }

                // Run the accuracy check pipeline using the correct method signature
                var result = await _accuracyService.CheckTranslationAccuracyAsync(
                    originalFilePath,
                    translatedFilePath,
                    sourceLanguage,
                    targetLanguage,
                    targetCountry
                );

                _logger.LogInformation($"Accuracy check completed. Overall score: {result.AccuracyScore}%, Similarity: {result.SemanticSimilarity:F2}");

                // *** NEW: Save accuracy results to session for persistence ***
                await SaveAccuracyResultsToSession(originalFile, translatedFile, result, targetCountry, sessionId);

                return Json(new
                {
                    success = true,
                    data = result,
                    message = "Accuracy check completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation accuracy check");
                return Json(new
                {
                    success = false,
                    message = $"Error during accuracy check: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Save accuracy results to session for persistence across navigation
        /// </summary>
        private async Task SaveAccuracyResultsToSession(string originalFile, string translatedFile,
            TranslationAccuracyResult accuracyResult, string targetCountry, string sessionId = null)
        {
            try
            {
                // Try with provided sessionId
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var sessionKey = $"TranslationResult_{sessionId}";
                    var sessionData = HttpContext.Session.GetString(sessionKey);
                    if (!string.IsNullOrEmpty(sessionData))
                    {
                        var translationData = JsonConvert.DeserializeObject<TranslationSessionData>(sessionData);

                        // Update session with accuracy results
                        translationData.AccuracyResults = accuracyResult;
                        translationData.AccuracyCheckDate = DateTime.Now;
                        translationData.TargetCountry = targetCountry;

                        // Save back to session
                        HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(translationData));

                        _logger.LogInformation($"Saved accuracy results to session {sessionKey}");
                        return;
                    }
                }

                // Fallback: Use a session lookup by filename (less efficient but works)
                // This is for cases where sessionId is not provided
                var lookupKey = $"AccuracyResults_{originalFile}_{translatedFile}";
                var tempAccuracyData = new
                {
                    AccuracyResults = accuracyResult,
                    AccuracyCheckDate = DateTime.Now,
                    TargetCountry = targetCountry,
                    OriginalFile = originalFile,
                    TranslatedFile = translatedFile
                };

                HttpContext.Session.SetString(lookupKey, JsonConvert.SerializeObject(tempAccuracyData));
                _logger.LogInformation($"Saved accuracy results to temporary session key {lookupKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving accuracy results to session");
            }
        }

        /// <summary>
        /// Get accuracy check status (for progress tracking)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAccuracyCheckStatus(string sessionId)
        {
            // This can be implemented if you want to track progress
            // For now, return a simple status
            return Json(new
            {
                success = true,
                status = "completed",
                message = "Accuracy check status retrieved"
            });
        }

        /// <summary>
        /// Download accuracy report as JSON
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadAccuracyReport(string originalFile, string translatedFile)
        {
            try
            {
                // This would typically get a cached result or re-run the check
                // For now, return a simple implementation
                var reportData = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    OriginalFile = originalFile,
                    TranslatedFile = translatedFile,
                    Message = "This is a placeholder for the accuracy report. Implement caching to store previous results."
                };

                var json = JsonConvert.SerializeObject(reportData, Formatting.Indented);
                var fileName = $"accuracy_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating accuracy report");
                return Json(new { success = false, message = "Error generating report" });
            }
        }

        /// <summary>
        /// Helper method to get default country for a language
        /// </summary>
        private string GetDefaultCountryForLanguage(string language)
        {
            return language.ToLower() switch
            {
                "vietnamese" => "Vietnam",
                "english" => "United States",
                "spanish" => "Spain",
                "french" => "France",
                "german" => "Germany",
                "chinese" => "China",
                "japanese" => "Japan",
                "korean" => "South Korea",
                "portuguese" => "Portugal",
                "russian" => "Russia",
                "arabic" => "Saudi Arabia",
                "hindi" => "India",
                "thai" => "Thailand",
                "italian" => "Italy",
                "dutch" => "Netherlands",
                _ => "United States"
            };
        }

        /// <summary>
        /// Get available target countries for cultural context
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetTargetCountries()
        {
            var countries = new[]
            {
                "Vietnam", "United States", "United Kingdom", "Canada", "Australia",
                "Spain", "Mexico", "Argentina", "France", "Germany", "Italy",
                "China", "Japan", "South Korea", "Thailand", "India",
                "Brazil", "Portugal", "Russia", "Saudi Arabia", "Netherlands"
            };

            return Json(new { success = true, countries = countries });
        }
    }

    // *** NEW: Model class to store translation session data ***
    public class TranslationSessionData
    {
        public string SessionId { get; set; }
        public string OriginalSrtFileName { get; set; }
        public string TranslatedSrtFileName { get; set; }
        public string TargetLanguage { get; set; }
        public string OriginalFileName { get; set; }
        public string SourceLanguage { get; set; }
        public int TranscriptionLength { get; set; }
        public int SegmentCount { get; set; }
        public DateTime ProcessedDate { get; set; }

        // *** NEW: Add accuracy check results ***
        public TranslationAccuracyResult AccuracyResults { get; set; }
        public DateTime? AccuracyCheckDate { get; set; }
        public string TargetCountry { get; set; }
    }
}