using Film_website.Models;
using System.Text.RegularExpressions;

namespace Film_website.Services
{
    public class SubtitleService : ISubtitleService
    {
        private readonly ILogger<SubtitleService> _logger;

        public SubtitleService(ILogger<SubtitleService> logger)
        {
            _logger = logger;
        }

        public async Task<SubtitleParseResult> ParseSubtitleFileAsync(IFormFile file)
        {
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync();
                return ParseSubtitleTextAsync(content).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing subtitle file: {FileName}", file.FileName);
                return new SubtitleParseResult
                {
                    Success = false,
                    ErrorMessage = $"Error parsing file: {ex.Message}"
                };
            }
        }

        public async Task<SubtitleParseResult> ParseSubtitleTextAsync(string content)
        {
            try
            {
                var paragraphs = new List<SubtitleParagraphViewModel>();

                if (content.Contains("-->"))
                {
                    // Parse SRT format
                    paragraphs = ParseSrtFormat(content);
                }
                else if (content.Contains("WEBVTT"))
                {
                    // Parse VTT format
                    paragraphs = ParseVttFormat(content);
                }
                else
                {
                    return new SubtitleParseResult
                    {
                        Success = false,
                        ErrorMessage = "Unsupported subtitle format"
                    };
                }

                return new SubtitleParseResult
                {
                    Success = true,
                    Paragraphs = paragraphs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing subtitle text");
                return new SubtitleParseResult
                {
                    Success = false,
                    ErrorMessage = $"Error parsing subtitle: {ex.Message}"
                };
            }
        }

        private List<SubtitleParagraphViewModel> ParseSrtFormat(string content)
        {
            var paragraphs = new List<SubtitleParagraphViewModel>();
            var blocks = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                var lines = block.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 3)
                {
                    if (int.TryParse(lines[0].Trim(), out int number))
                    {
                        var timeLine = lines[1].Trim();
                        var textLines = lines.Skip(2).ToArray();

                        if (timeLine.Contains("-->"))
                        {
                            var times = timeLine.Split(new[] { "-->" }, StringSplitOptions.None);
                            if (times.Length == 2)
                            {
                                paragraphs.Add(new SubtitleParagraphViewModel
                                {
                                    Number = number,
                                    StartTime = times[0].Trim(),
                                    EndTime = times[1].Trim(),
                                    Text = string.Join(" ", textLines)
                                });
                            }
                        }
                    }
                }
            }

            return paragraphs;
        }

        private List<SubtitleParagraphViewModel> ParseVttFormat(string content)
        {
            var paragraphs = new List<SubtitleParagraphViewModel>();
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int number = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("-->"))
                {
                    var times = line.Split(new[] { "-->" }, StringSplitOptions.None);
                    if (times.Length == 2 && i + 1 < lines.Length)
                    {
                        var text = lines[i + 1].Trim();
                        paragraphs.Add(new SubtitleParagraphViewModel
                        {
                            Number = number++,
                            StartTime = times[0].Trim(),
                            EndTime = times[1].Trim(),
                            Text = text
                        });
                        i++; // Skip the text line
                    }
                }
            }

            return paragraphs;
        }

        public string GenerateSrtContent(List<SubtitleParagraphViewModel> paragraphs)
        {
            var result = new System.Text.StringBuilder();

            foreach (var paragraph in paragraphs.OrderBy(p => p.Number))
            {
                result.AppendLine(paragraph.Number.ToString());
                result.AppendLine($"{paragraph.StartTime} --> {paragraph.EndTime}");
                result.AppendLine(paragraph.Text);
                result.AppendLine();
            }

            return result.ToString();
        }

        public List<LanguageOption> GetSupportedLanguages()
        {
            return new List<LanguageOption>
            {
                new() { Code = "auto", Name = "Auto Detect" },
                new() { Code = "en", Name = "English" },
                new() { Code = "es", Name = "Spanish" },
                new() { Code = "fr", Name = "French" },
                new() { Code = "de", Name = "German" },
                new() { Code = "it", Name = "Italian" },
                new() { Code = "pt", Name = "Portuguese" },
                new() { Code = "zh", Name = "Chinese" },
                new() { Code = "ja", Name = "Japanese" },
                new() { Code = "ko", Name = "Korean" },
                new() { Code = "vi", Name = "Vietnamese" },
                new() { Code = "ru", Name = "Russian" },
                new() { Code = "ar", Name = "Arabic" },
                new() { Code = "hi", Name = "Hindi" },
                new() { Code = "th", Name = "Thai" }
            };
        }

        public bool IsValidSubtitleFile(string fileName)
        {
            var allowedExtensions = new[] { ".srt", ".vtt", ".ass", ".ssa", ".sub" };
            var extension = Path.GetExtension(fileName).ToLower();
            return allowedExtensions.Contains(extension);
        }
    }
}