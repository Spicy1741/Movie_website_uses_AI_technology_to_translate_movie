using System.Text;
using Newtonsoft.Json;
using Film_website.Models;

namespace Film_website.Services
{
    public class WhisperService : IWhisperService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhisperService> _logger;
        private readonly IConfiguration _configuration;

        // Language mapping for Whisper API
        private readonly Dictionary<string, string> _languageMapping = new()
        {
            { "Vietnamese", "vi" },
            { "English", "en" },
            { "Spanish", "es" },
            { "French", "fr" },
            { "German", "de" },
            { "Italian", "it" },
            { "Portuguese", "pt" },
            { "Russian", "ru" },
            { "Japanese", "ja" },
            { "Korean", "ko" },
            { "Chinese", "zh" },
            { "Arabic", "ar" },
            { "Hindi", "hi" },
            { "Thai", "th" },
            { "Indonesian", "id" }
        };

        public WhisperService(HttpClient httpClient, ILogger<WhisperService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Increased timeout for larger files
        }

        public async Task<TranscriptionResponse> TranscribeAudioAsync(string audioFilePath, string language = "auto")
        {
            try
            {
                if (!File.Exists(audioFilePath))
                    throw new FileNotFoundException($"Audio file not found: {audioFilePath}");

                var languageCode = GetLanguageCode(language);
                _logger.LogInformation($"Starting Whisper transcription for: {audioFilePath}");
                _logger.LogInformation($"Language: {language} -> {languageCode}");

                // Check file size (Whisper has 25MB limit)
                var fileInfo = new FileInfo(audioFilePath);
                if (fileInfo.Length > 25 * 1024 * 1024)
                {
                    throw new InvalidOperationException($"Audio file too large: {fileInfo.Length / (1024 * 1024)}MB. Maximum is 25MB.");
                }

                using var form = new MultipartFormDataContent();
                var fileBytes = await File.ReadAllBytesAsync(audioFilePath);
                var fileContent = new ByteArrayContent(fileBytes);

                // Set proper content type based on file extension
                var extension = Path.GetExtension(audioFilePath).ToLower();
                var contentType = extension switch
                {
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".m4a" => "audio/mp4",
                    ".ogg" => "audio/ogg",
                    _ => "audio/wav" // Default to WAV
                };

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent("whisper-1"), "model");

                // FIXED: Use verbose_json for detailed segment information with timestamps
                form.Add(new StringContent("verbose_json"), "response_format");

                // FIXED: Add word-level timestamps for better precision
                form.Add(new StringContent("segment"), "timestamp_granularities[]");
                form.Add(new StringContent("word"), "timestamp_granularities[]");

                // Set language if not auto
                if (languageCode != "auto")
                {
                    form.Add(new StringContent(languageCode), "language");
                    _logger.LogInformation($"Explicitly setting language to: {languageCode}");
                }
                else
                {
                    _logger.LogInformation("Using auto-detection for language");
                }

                // FIXED: Add temperature for more consistent results and timing
                form.Add(new StringContent("0.2"), "temperature"); // Slightly higher for better accuracy

                // FIXED: Add additional parameters for better timestamp accuracy
                form.Add(new StringContent("true"), "word_timestamps");

                _logger.LogInformation("Sending request to OpenAI Whisper API...");
                var startTime = DateTime.Now;

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                var responseContent = await response.Content.ReadAsStringAsync();

                var processingTime = DateTime.Now - startTime;
                _logger.LogInformation($"Whisper API call completed in {processingTime.TotalSeconds:F2} seconds");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Whisper API error: {response.StatusCode} - {responseContent}");

                    // Try to parse error details
                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        var errorMessage = errorObj?.error?.message?.ToString() ?? responseContent;
                        throw new HttpRequestException($"Whisper API error ({response.StatusCode}): {errorMessage}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException($"Whisper API error: {response.StatusCode} - {responseContent}");
                    }
                }

                _logger.LogInformation("Parsing Whisper response...");
                var whisperResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                // Extract basic information
                var fullText = whisperResponse?.text?.ToString() ?? "";
                var detectedLanguage = whisperResponse?.language?.ToString() ?? "unknown";
                var duration = whisperResponse?.duration?.ToString() ?? "0";

                _logger.LogInformation($"Transcription completed. Language detected: {detectedLanguage}, Duration: {duration}s, Text length: {fullText.Length} chars");

                if (string.IsNullOrWhiteSpace(fullText))
                {
                    _logger.LogWarning("Whisper returned empty transcription");
                    return new TranscriptionResponse
                    {
                        Success = false,
                        Message = "No speech detected in the audio file. Please ensure the audio contains clear speech."
                    };
                }

                // FIXED: Enhanced segment processing with timestamp validation
                var segments = new List<SrtEntry>();
                if (whisperResponse?.segments != null)
                {
                    int index = 1;
                    double lastEndTime = 0;

                    foreach (var segment in whisperResponse.segments)
                    {
                        var segmentText = segment.text?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(segmentText))
                        {
                            var startSeconds = Convert.ToDouble(segment.start ?? lastEndTime);
                            var endSeconds = Convert.ToDouble(segment.end ?? startSeconds + 1);

                            // FIXED: Validate and adjust timestamps to prevent overlaps and gaps
                            if (startSeconds < lastEndTime)
                            {
                                _logger.LogDebug($"Adjusting segment {index} start time from {startSeconds:F3}s to {lastEndTime:F3}s to prevent overlap");
                                startSeconds = lastEndTime;
                            }

                            if (endSeconds <= startSeconds)
                            {
                                endSeconds = startSeconds + 0.5; // Minimum 0.5 second duration
                                _logger.LogDebug($"Adjusting segment {index} end time to {endSeconds:F3}s to ensure minimum duration");
                            }

                            // FIXED: Ensure segments don't have unrealistic gaps (more than 2 seconds)
                            if (index > 1 && (startSeconds - lastEndTime) > 2.0)
                            {
                                _logger.LogDebug($"Large gap detected before segment {index}: {startSeconds - lastEndTime:F3}s");
                            }

                            segments.Add(new SrtEntry
                            {
                                Index = index++,
                                StartTime = TimeSpan.FromSeconds(startSeconds),
                                EndTime = TimeSpan.FromSeconds(endSeconds),
                                Text = segmentText
                            });

                            lastEndTime = endSeconds;
                        }
                    }

                    // FIXED: Post-process segments to ensure proper timing
                    segments = OptimizeSegmentTiming(segments);
                }
                else
                {
                    // Fallback: create single segment if no segments provided
                    _logger.LogWarning("No segments in Whisper response, creating single segment");
                    segments.Add(new SrtEntry
                    {
                        Index = 1,
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.FromSeconds(Convert.ToDouble(duration)),
                        Text = fullText
                    });
                }

                _logger.LogInformation($"Created {segments.Count} subtitle segments with validated timestamps");

                // FIXED: Validate overall timing consistency
                if (segments.Count > 0)
                {
                    var totalSegmentDuration = segments.Last().EndTime.TotalSeconds;
                    var expectedDuration = Convert.ToDouble(duration);
                    var timingError = Math.Abs(totalSegmentDuration - expectedDuration);

                    if (timingError > 1.0)
                    {
                        _logger.LogWarning($"Timing validation warning: Expected {expectedDuration:F2}s, got {totalSegmentDuration:F2}s (diff: {timingError:F2}s)");
                    }
                    else
                    {
                        _logger.LogInformation($"Timing validation passed: {totalSegmentDuration:F2}s vs expected {expectedDuration:F2}s");
                    }
                }

                return new TranscriptionResponse
                {
                    Success = true,
                    Text = fullText,
                    Segments = segments,
                    ProcessingTime = processingTime,
                    Message = $"Transcription completed successfully. Detected language: {detectedLanguage}, Timing validated: {segments.Count} segments"
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Whisper API request timed out");
                return new TranscriptionResponse
                {
                    Success = false,
                    Message = "Transcription timed out. Please try with a shorter audio file."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Whisper transcription");
                return new TranscriptionResponse
                {
                    Success = false,
                    Message = $"Transcription failed: {ex.Message}"
                };
            }
        }

        // NEW: Method to optimize segment timing and fix common issues
        private List<SrtEntry> OptimizeSegmentTiming(List<SrtEntry> segments)
        {
            if (segments.Count == 0) return segments;

            var optimized = new List<SrtEntry>();

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var newSegment = new SrtEntry
                {
                    Index = segment.Index,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime,
                    Text = segment.Text
                };

                // Ensure minimum segment duration (0.5 seconds)
                var minDuration = TimeSpan.FromSeconds(0.5);
                if (newSegment.EndTime - newSegment.StartTime < minDuration)
                {
                    newSegment.EndTime = newSegment.StartTime + minDuration;
                }

                // Prevent overlaps with next segment
                if (i < segments.Count - 1)
                {
                    var nextSegment = segments[i + 1];
                    if (newSegment.EndTime > nextSegment.StartTime)
                    {
                        // Create small gap between segments
                        var gap = TimeSpan.FromMilliseconds(100);
                        newSegment.EndTime = nextSegment.StartTime - gap;

                        // Ensure we don't make the segment too short
                        if (newSegment.EndTime <= newSegment.StartTime)
                        {
                            newSegment.EndTime = newSegment.StartTime + minDuration;
                        }
                    }
                }

                optimized.Add(newSegment);
            }

            return optimized;
        }

        public async Task<string> GenerateSrtAsync(string audioFilePath, string language = "auto")
        {
            var response = await TranscribeAudioAsync(audioFilePath, language);
            if (!response.Success || !response.Segments.Any())
                return "";

            var srtBuilder = new StringBuilder();
            foreach (var segment in response.Segments)
            {
                srtBuilder.AppendLine(segment.Index.ToString());
                srtBuilder.AppendLine($"{FormatTime(segment.StartTime)} --> {FormatTime(segment.EndTime)}");
                srtBuilder.AppendLine(segment.Text);
                srtBuilder.AppendLine();
            }

            return srtBuilder.ToString();
        }

        private string GetLanguageCode(string language)
        {
            if (language == "auto" || string.IsNullOrEmpty(language))
                return "auto";

            // If it's already a language code (2-3 characters), return as is
            if (language.Length <= 3 && _languageMapping.ContainsValue(language.ToLower()))
                return language.ToLower();

            // Try to find the language code from the mapping
            if (_languageMapping.TryGetValue(language, out var code))
                return code;

            // Try case-insensitive search
            var foundMapping = _languageMapping.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, language, StringComparison.OrdinalIgnoreCase));

            if (!foundMapping.Equals(default(KeyValuePair<string, string>)))
                return foundMapping.Value;

            // If not found, try to use the language as-is (might be a valid ISO code)
            _logger.LogWarning($"Unknown language '{language}', using as-is");
            return language.ToLower();
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }
    }
}