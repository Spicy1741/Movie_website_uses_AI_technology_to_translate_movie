using Film_website.Models;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;

namespace Film_website.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SrtEditorController : Controller
    {
        private readonly IGptTranslationService _translationService;
        private readonly ILogger<SrtEditorController> _logger;

        public SrtEditorController(
            IGptTranslationService translationService,
            ILogger<SrtEditorController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        // Display SRT Editor Interface
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "SRT Editor - Film Website Admin";
            return View();
        }

        // Translate Subtitle Content
        [HttpPost]
        public async Task<IActionResult> TranslateSubtitle(string subtitleContent, string sourceLanguage = "auto", string targetLanguage = "Vietnamese")
        {
            try
            {
                _logger.LogInformation("=== STARTING SRT SUBTITLE TRANSLATION ===");
                _logger.LogInformation($"Source Language: {sourceLanguage}, Target Language: {targetLanguage}");
                _logger.LogInformation($"Content Length: {subtitleContent?.Length ?? 0} characters");

                if (string.IsNullOrWhiteSpace(subtitleContent))
                {
                    return Json(new { success = false, message = "No subtitle content provided" });
                }

                // Parse subtitle content
                var subtitleEntries = ParseSrtContent(subtitleContent);
                if (subtitleEntries.Count == 0)
                {
                    return Json(new { success = false, message = "No valid subtitle entries found" });
                }

                _logger.LogInformation($"Parsed {subtitleEntries.Count} subtitle entries");

                // Extract text for translation
                var textsToTranslate = subtitleEntries.Select(entry => entry.Text).ToList();

                // Translate the texts
                var translatedTexts = await TranslateTexts(textsToTranslate, sourceLanguage, targetLanguage);

                if (translatedTexts == null || translatedTexts.Count != textsToTranslate.Count)
                {
                    return Json(new { success = false, message = "Translation failed" });
                }

                // Create translated subtitle entries
                for (int i = 0; i < subtitleEntries.Count; i++)
                {
                    subtitleEntries[i].Text = translatedTexts[i];
                }

                // Generate translated SRT content
                var translatedContent = GenerateSrtContent(subtitleEntries);

                _logger.LogInformation("SRT subtitle translation completed successfully");

                return Json(new
                {
                    success = true,
                    translatedContent = translatedContent,
                    message = "Translation completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SRT subtitle translation");
                return Json(new { success = false, message = "Translation failed: " + ex.Message });
            }
        }

        // Validate SRT file format
        [HttpPost]
        public IActionResult ValidateSrtFile(IFormFile srtFile)
        {
            try
            {
                if (srtFile == null || srtFile.Length == 0)
                {
                    return Json(new { success = false, message = "No file provided" });
                }

                // Check file extension
                var allowedExtensions = new[] { ".srt", ".vtt", ".ass", ".ssa", ".sub" };
                var fileExtension = Path.GetExtension(srtFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Invalid file type. Please upload .srt, .vtt, .ass, .ssa, or .sub files only." });
                }

                // Check file size (max 20MB)
                if (srtFile.Length > 20 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File too large. Maximum size is 20MB." });
                }

                // Read and validate content
                using var reader = new StreamReader(srtFile.OpenReadStream());
                var content = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "File is empty" });
                }

                // Basic SRT format validation
                var entries = ParseSrtContent(content);
                if (entries.Count == 0)
                {
                    return Json(new { success = false, message = "No valid subtitle entries found in the file" });
                }

                return Json(new
                {
                    success = true,
                    content = content,
                    entryCount = entries.Count,
                    message = $"Valid subtitle file with {entries.Count} entries"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SRT file");
                return Json(new { success = false, message = "Error reading file: " + ex.Message });
            }
        }

        // Format SRT content
        [HttpPost]
        public IActionResult FormatSrtContent(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "No content provided" });
                }

                var entries = ParseSrtContent(content);
                if (entries.Count == 0)
                {
                    return Json(new { success = false, message = "No valid subtitle entries found" });
                }

                var formattedContent = GenerateSrtContent(entries);

                return Json(new
                {
                    success = true,
                    formattedContent = formattedContent,
                    message = "Content formatted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting SRT content");
                return Json(new { success = false, message = "Error formatting content: " + ex.Message });
            }
        }

        // Parse SRT content into structured data
        private List<SrtSubtitleEntry> ParseSrtContent(string content)
        {
            var entries = new List<SrtSubtitleEntry>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            SrtSubtitleEntry? currentEntry = null;
            bool readingText = false;
            var textLines = new List<string>();
            int lineIndex = 0;

            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex].Trim();

                // Empty line - end of current entry
                if (string.IsNullOrEmpty(line))
                {
                    if (currentEntry != null && textLines.Count > 0)
                    {
                        currentEntry.Text = string.Join("\n", textLines);
                        entries.Add(currentEntry);
                        currentEntry = null;
                        textLines.Clear();
                        readingText = false;
                    }
                    lineIndex++;
                    continue;
                }

                // Check if it's a subtitle number
                if (int.TryParse(line, out int number) && !readingText)
                {
                    currentEntry = new SrtSubtitleEntry { Number = number };
                    lineIndex++;
                    continue;
                }

                // Check if it's a timestamp
                if (line.Contains("-->") && currentEntry != null && !readingText)
                {
                    currentEntry.Timestamp = line;
                    readingText = true;
                    lineIndex++;
                    continue;
                }

                // It's subtitle text
                if (readingText && currentEntry != null)
                {
                    textLines.Add(line);
                }

                lineIndex++;
            }

            // Add the last entry if exists
            if (currentEntry != null && textLines.Count > 0)
            {
                currentEntry.Text = string.Join("\n", textLines);
                entries.Add(currentEntry);
            }

            return entries;
        }

        // Generate SRT content from subtitle entries
        private string GenerateSrtContent(List<SrtSubtitleEntry> entries)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Ensure proper numbering
                sb.AppendLine((i + 1).ToString());
                sb.AppendLine(entry.Timestamp);
                sb.AppendLine(entry.Text);
                sb.AppendLine(); // Empty line between entries
            }

            return sb.ToString().TrimEnd();
        }

        // Translate multiple texts using the translation service
        private async Task<List<string>> TranslateTexts(List<string> texts, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var translatedTexts = new List<string>();

                // Translate in smaller batches to avoid API limits
                const int batchSize = 5;
                for (int i = 0; i < texts.Count; i += batchSize)
                {
                    var batch = texts.Skip(i).Take(batchSize).ToList();
                    var batchResults = new List<string>();

                    foreach (var text in batch)
                    {
                        try
                        {
                            // Clean text before translation (remove HTML tags, extra spaces)
                            var cleanText = CleanSubtitleText(text);

                            var translatedText = await _translationService.TranslateTextAsync(cleanText, targetLanguage);
                            batchResults.Add(translatedText ?? text); // Fallback to original if translation fails

                            _logger.LogDebug($"Translated: '{cleanText}' -> '{translatedText}'");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to translate text: {text}");
                            batchResults.Add(text); // Use original text as fallback
                        }
                    }

                    translatedTexts.AddRange(batchResults);

                    // Small delay between batches to be respectful to the API
                    if (i + batchSize < texts.Count)
                    {
                        await Task.Delay(300);
                    }
                }

                return translatedTexts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating subtitle texts");
                return texts; // Return original texts as fallback
            }
        }

        // Clean subtitle text for better translation
        private string CleanSubtitleText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Remove common subtitle formatting
            text = text.Replace("<i>", "").Replace("</i>", "");
            text = text.Replace("<b>", "").Replace("</b>", "");
            text = text.Replace("<u>", "").Replace("</u>", "");

            // Remove extra whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }

        // Get supported languages for the dropdown
        [HttpGet]
        public IActionResult GetSupportedLanguages()
        {
            var languages = new Dictionary<string, string>
            {
                { "auto", "Auto Detect" },
                { "en", "English" },
                { "vi", "Vietnamese" },
                { "zh", "Chinese (Simplified)" },
                { "zh-TW", "Chinese (Traditional)" },
                { "ja", "Japanese" },
                { "ko", "Korean" },
                { "fr", "French" },
                { "es", "Spanish" },
                { "de", "German" },
                { "it", "Italian" },
                { "pt", "Portuguese" },
                { "ru", "Russian" },
                { "ar", "Arabic" },
                { "hi", "Hindi" },
                { "th", "Thai" },
                { "ms", "Malay" },
                { "id", "Indonesian" }
            };

            return Json(new { success = true, languages = languages });
        }
    }

    // SRT Subtitle Entry Model
    public class SrtSubtitleEntry
    {
        public int Number { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}