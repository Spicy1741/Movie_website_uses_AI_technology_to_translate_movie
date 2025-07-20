using Film_website.Models;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Film_website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TranslatorController : ControllerBase
    {
        private readonly IWhisperService? _whisperService;
        private readonly ITranslationService? _translationService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TranslatorController> _logger;
        private readonly UserService _userService;
        private readonly UserActivityService? _activityService;

        public TranslatorController(
            IWebHostEnvironment environment,
            ILogger<TranslatorController> logger,
            UserService userService,
            IWhisperService? whisperService = null,
            ITranslationService? translationService = null,
            UserActivityService? activityService = null)
        {
            _environment = environment;
            _logger = logger;
            _userService = userService;
            _whisperService = whisperService;
            _translationService = translationService;
            _activityService = activityService;
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> TranscribeAndTranslate([FromForm] TranslationRequest request)
        {
            if (_whisperService == null)
            {
                return BadRequest(new TranslationResponse
                {
                    Success = false,
                    Message = "Whisper service not available"
                });
            }

            try
            {
                if (request.VideoFile == null || request.VideoFile.Length == 0)
                {
                    return BadRequest(new TranslationResponse
                    {
                        Success = false,
                        Message = "No file uploaded"
                    });
                }

                // Validate file type
                var allowedExtensions = new[] { ".mp4", ".mp3", ".wav", ".m4a", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".aac", ".ogg", ".flac" };
                var fileExtension = Path.GetExtension(request.VideoFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new TranslationResponse
                    {
                        Success = false,
                        Message = "Invalid file type. Please upload a supported audio/video file."
                    });
                }

                // Check file size (limit to 500MB)
                if (request.VideoFile.Length > 524288000)
                {
                    return BadRequest(new TranslationResponse
                    {
                        Success = false,
                        Message = "File size too large. Maximum size is 500MB."
                    });
                }

                // Save uploaded file
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}_{request.VideoFile.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.VideoFile.CopyToAsync(stream);
                }

                // Transcribe using Whisper AI
                var transcriptionResult = await _whisperService.TranscribeAudioAsync(filePath, request.SourceLanguage);

                if (!transcriptionResult.Success)
                {
                    // Clean up uploaded file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    return BadRequest(transcriptionResult);
                }

                // Optional: Translate if enabled
                if (request.EnableTranslation && _translationService != null && !string.IsNullOrEmpty(transcriptionResult.OriginalText))
                {
                    transcriptionResult.TranslatedText = await _translationService.TranslateSubtitleAsync(
                        transcriptionResult.OriginalText,
                        request.SourceLanguage,
                        request.TargetLanguage);
                }

                // Save results
                var downloadsPath = Path.Combine(_environment.WebRootPath, "downloads");
                Directory.CreateDirectory(downloadsPath);

                var resultFileName = $"{Path.GetFileNameWithoutExtension(request.VideoFile.FileName)}_transcription.srt";
                var resultFilePath = Path.Combine(downloadsPath, resultFileName);

                var contentToSave = request.EnableTranslation && !string.IsNullOrEmpty(transcriptionResult.TranslatedText)
                    ? transcriptionResult.TranslatedText
                    : transcriptionResult.OriginalText;

                await System.IO.File.WriteAllTextAsync(resultFilePath, contentToSave ?? "");

                transcriptionResult.DownloadUrl = $"/downloads/{resultFileName}";
                transcriptionResult.FileName = resultFileName;

                // Log activity
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogActivityAsync(
                            adminUser.Id,
                            "Whisper Transcription",
                            $"Transcribed: {request.VideoFile.FileName}"
                        );
                    }
                }

                // Clean up uploaded file
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary file: {FilePath}", filePath);
                }

                return Ok(transcriptionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transcription and translation");
                return StatusCode(500, new TranslationResponse
                {
                    Success = false,
                    Message = "Internal server error during processing",
                    Errors = { ex.Message }
                });
            }
        }

        [HttpPost("upload-srt")]
        public async Task<IActionResult> UploadSrtFile([FromForm] IFormFile srtFile)
        {
            try
            {
                if (srtFile == null || srtFile.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No SRT file selected" });
                }

                // Validate file type
                var fileExtension = Path.GetExtension(srtFile.FileName).ToLower();
                if (!new[] { ".srt", ".vtt" }.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = "Please upload a valid .srt or .vtt file" });
                }

                // Read SRT content
                using var reader = new StreamReader(srtFile.OpenReadStream());
                var srtContent = await reader.ReadToEndAsync();

                // Log activity
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogActivityAsync(
                            adminUser.Id,
                            "SRT Upload",
                            $"Uploaded SRT file: {srtFile.FileName}"
                        );
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "SRT file uploaded successfully",
                    content = srtContent,
                    fileName = srtFile.FileName,
                    size = srtFile.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading SRT file");
                return BadRequest(new { success = false, message = "Error uploading SRT file" });
            }
        }

        [HttpGet("languages")]
        public IActionResult GetSupportedLanguages()
        {
            var languages = new Dictionary<string, string>
            {
                { "en", "English" },
                { "es", "Spanish" },
                { "fr", "French" },
                { "de", "German" },
                { "it", "Italian" },
                { "pt", "Portuguese" },
                { "zh", "Chinese" },
                { "ja", "Japanese" },
                { "ko", "Korean" },
                { "vi", "Vietnamese" }
            };

            return Ok(languages);
        }

        [HttpPost("save-edited")]
        public async Task<IActionResult> SaveEditedSubtitle([FromBody] SaveEditedRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Content))
                {
                    return BadRequest(new { success = false, message = "No content to save" });
                }

                var downloadsPath = Path.Combine(_environment.WebRootPath, "downloads");
                Directory.CreateDirectory(downloadsPath);

                var fileName = $"edited_subtitle_{DateTime.Now:yyyyMMdd_HHmmss}.srt";
                var filePath = Path.Combine(downloadsPath, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, request.Content);

                // Log activity
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogActivityAsync(
                            adminUser.Id,
                            "Subtitle Edit",
                            $"Saved edited subtitle: {fileName}"
                        );
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Subtitle saved successfully",
                    downloadUrl = $"/downloads/{fileName}",
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving edited subtitle");
                return StatusCode(500, new { success = false, message = "Error saving subtitle" });
            }
        }

        [HttpPost("clear")]
        public IActionResult ClearFiles()
        {
            try
            {
                // Clear temporary files if needed
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (Directory.Exists(uploadsPath))
                {
                    var files = Directory.GetFiles(uploadsPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.CreationTime < DateTime.Now.AddHours(-1)) // Only delete files older than 1 hour
                            {
                                System.IO.File.Delete(file);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete file: {File}", file);
                        }
                    }
                }

                return Ok(new { success = true, message = "Files cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing files");
                return StatusCode(500, new { success = false, message = "Error clearing files" });
            }
        }
    }

    // Request model for save edited endpoint
    public class SaveEditedRequest
    {
        public string Content { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}