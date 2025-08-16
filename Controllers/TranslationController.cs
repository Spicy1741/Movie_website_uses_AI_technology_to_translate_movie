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

        // *** NEW: Add accuracy service dependency ***
        private readonly ITranslationAccuracyService _accuracyService;

        public TranslationController(
            IWhisperService whisperService,
            IGptTranslationService translationService,
            IAudioExtractionService audioExtractionService,
            ISrtGeneratorService srtGeneratorService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<TranslationController> logger,
            ITranslationAccuracyService accuracyService) // *** NEW: Add accuracy service parameter ***
        {
            _whisperService = whisperService;
            _translationService = translationService;
            _audioExtractionService = audioExtractionService;
            _srtGeneratorService = srtGeneratorService;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
            _accuracyService = accuracyService; // *** NEW: Initialize accuracy service ***
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

                if (string.IsNullOrEmpty(targetLanguage))
                {
                    _logger.LogError("Target language not specified");
                    ViewBag.Error = "Please select a target language.";
                    return View("Upload");
                }

                // Validate OpenAI API configuration
                if (!ValidateOpenAIConfiguration())
                {
                    ViewBag.Error = "OpenAI API is not properly configured. Please contact the administrator.";
                    return View("Upload");
                }

                // Create paths for uploads and downloads
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                var downloadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "downloads");

                // Create directories if they don't exist
                Directory.CreateDirectory(uploadsPath);
                Directory.CreateDirectory(downloadsPath);

                // Save uploaded video file
                var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(videoFile.FileName));
                var fileExtension = Path.GetExtension(videoFile.FileName);
                videoFilePath = Path.Combine(uploadsPath, $"{sanitizedFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}");

                _logger.LogInformation($"Saving video to: {videoFilePath}");
                using (var stream = new FileStream(videoFilePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                if (!System.IO.File.Exists(videoFilePath))
                {
                    _logger.LogError($"Failed to save video file: {videoFilePath}");
                    ViewBag.Error = "Failed to save the uploaded video file. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation("=== VIDEO SAVED SUCCESSFULLY ===");

                // Step 1: Extract audio from video
                _logger.LogInformation("=== STARTING AUDIO EXTRACTION ===");
                audioFilePath = await _audioExtractionService.ExtractAudioAsync(videoFilePath, uploadsPath);

                if (string.IsNullOrEmpty(audioFilePath) || !System.IO.File.Exists(audioFilePath))
                {
                    _logger.LogError($"Audio extraction failed. Expected path: {audioFilePath}");
                    CleanupFile(videoFilePath);
                    ViewBag.Error = "Failed to extract audio from the video file. Please ensure the video file is valid.";
                    return View("Upload");
                }

                _logger.LogInformation($"Audio extracted successfully: {audioFilePath}");

                // Step 2: Transcribe audio using Whisper
                _logger.LogInformation("=== STARTING TRANSCRIPTION ===");
                var transcriptionResponse = await _whisperService.TranscribeAudioAsync(audioFilePath, sourceLanguage);

                if (!transcriptionResponse.Success)
                {
                    _logger.LogError($"Transcription failed: {transcriptionResponse.Message}");
                    CleanupFile(videoFilePath);
                    CleanupFile(audioFilePath);
                    ViewBag.Error = $"Transcription failed: {transcriptionResponse.Message}";
                    return View("Upload");
                }

                _logger.LogInformation($"Transcription completed. Text length: {transcriptionResponse.Text.Length}, Segments: {transcriptionResponse.Segments.Count}");

                // Step 3: Generate SRT files with translation
                _logger.LogInformation("=== GENERATING SRT FILES WITH TRANSLATION ===");

                // Generate original SRT
                var originalSrt = _srtGeneratorService.GenerateSrt(transcriptionResponse.Segments);
                var originalSrtFileName = $"{sanitizedFileName}_original_{DateTime.Now:yyyyMMdd_HHmmss}.srt";
                var originalSrtPath = Path.Combine(downloadsPath, originalSrtFileName);
                await System.IO.File.WriteAllTextAsync(originalSrtPath, originalSrt, System.Text.Encoding.UTF8);

                // Generate translated SRT by translating each segment individually
                var translatedSegments = new List<SrtEntry>();

                _logger.LogInformation("=== TRANSLATING INDIVIDUAL SEGMENTS ===");
                for (int i = 0; i < transcriptionResponse.Segments.Count; i++)
                {
                    var segment = transcriptionResponse.Segments[i];
                    string translatedSegmentText;

                    try
                    {
                        // Translate each segment individually for better accuracy
                        translatedSegmentText = await _translationService.TranslateTextAsync(
                            segment.Text,
                            targetLanguage,
                            sourceLanguage == "auto" ? "English" : sourceLanguage
                        );

                        if (string.IsNullOrEmpty(translatedSegmentText))
                        {
                            translatedSegmentText = segment.Text; // Fallback to original
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to translate segment {i + 1}, using original text");
                        translatedSegmentText = segment.Text; // Fallback to original
                    }

                    translatedSegments.Add(new SrtEntry
                    {
                        Index = segment.Index,
                        StartTime = segment.StartTime,
                        EndTime = segment.EndTime,
                        Text = segment.Text,
                        TranslatedText = translatedSegmentText
                    });

                    // Small delay to avoid rate limiting
                    if (i < transcriptionResponse.Segments.Count - 1)
                    {
                        await Task.Delay(100);
                    }
                }

                var translatedSrt = _srtGeneratorService.GenerateSrt(translatedSegments);
                var translatedSrtFileName = $"{sanitizedFileName}_translated_{targetLanguage.ToLower()}_{DateTime.Now:yyyyMMdd_HHmmss}.srt";
                var translatedSrtPath = Path.Combine(downloadsPath, translatedSrtFileName);
                await System.IO.File.WriteAllTextAsync(translatedSrtPath, translatedSrt, System.Text.Encoding.UTF8);

                // Verify files were created
                if (!System.IO.File.Exists(originalSrtPath) || !System.IO.File.Exists(translatedSrtPath))
                {
                    _logger.LogError($"SRT generation failed. Original: {System.IO.File.Exists(originalSrtPath)}, Translated: {System.IO.File.Exists(translatedSrtPath)}");
                    CleanupFile(videoFilePath);
                    CleanupFile(audioFilePath);
                    ViewBag.Error = "Failed to generate subtitle files. Please try again.";
                    return View("Upload");
                }

                _logger.LogInformation("=== SRT FILES SAVED SUCCESSFULLY ===");

                // Clean up temporary files
                CleanupFile(videoFilePath);
                CleanupFile(audioFilePath);

                // Set ViewBag data for Result view
                ViewBag.OriginalSrtPath = $"/Translation/DownloadFile?fileName={originalSrtFileName}";
                ViewBag.TranslatedSrtPath = $"/Translation/DownloadFile?fileName={translatedSrtFileName}";
                ViewBag.OriginalSrtFileName = originalSrtFileName; // *** NEW: Add original file name ***
                ViewBag.TranslatedSrtFileName = translatedSrtFileName; // *** NEW: Add translated file name ***
                ViewBag.TargetLanguage = targetLanguage;
                ViewBag.OriginalFileName = sanitizedFileName;
                ViewBag.SourceLanguage = sourceLanguage == "auto" ? "English" : sourceLanguage;
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
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return BadRequest("Error downloading file.");
            }
        }

        // *** EXISTING HELPER METHODS - UNCHANGED ***
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

        private bool ValidateOpenAIConfiguration()
        {
            var apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
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
                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitized = fileName;

                foreach (var c in invalidChars)
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
        // *** NEW ACCURACY CHECK METHODS - START ***
        // =========================================================

        /// <summary>
        /// Check translation accuracy using 3-layer pipeline
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAccuracy(string originalFile, string translatedFile,
            string sourceLanguage, string targetLanguage, string targetCountry = "")
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

                // Run the accuracy check pipeline
                var result = await _accuracyService.CheckTranslationAccuracyAsync(
                    originalFilePath,
                    translatedFilePath,
                    sourceLanguage,
                    targetLanguage,
                    targetCountry
                );

                _logger.LogInformation($"Accuracy check completed. Overall score: {result.AccuracyScore}%, Similarity: {result.SemanticSimilarity:F2}");

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

        // =========================================================
        // *** NEW ACCURACY CHECK METHODS - END ***
        // =========================================================
    }
}