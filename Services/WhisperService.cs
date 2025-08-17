using Film_website.Models;
using Newtonsoft.Json;
using System.Text;

namespace Film_website.Services
{
    public class WhisperService : IWhisperService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhisperService> _logger;
        private readonly string _apiKey;

        // Language code mapping for Whisper API
        private readonly Dictionary<string, string> _languageMapping = new()
        {
            {"Vietnamese", "vi"},
            {"English", "en"},
            {"Spanish", "es"},
            {"French", "fr"},
            {"German", "de"},
            {"Chinese", "zh"},
            {"Japanese", "ja"},
            {"Korean", "ko"},
            {"Portuguese", "pt"},
            {"Russian", "ru"},
            {"Arabic", "ar"},
            {"Hindi", "hi"},
            {"Thai", "th"},
            {"Italian", "it"},
            {"Dutch", "nl"}
        };

        public WhisperService(HttpClient httpClient, IConfiguration configuration, ILogger<WhisperService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Increase timeout for large files
        }

        public async Task<TranscriptionResponse> TranscribeAudioAsync(string audioFilePath, string language = "auto")
        {
            try
            {
                if (!File.Exists(audioFilePath))
                    throw new FileNotFoundException($"Audio file not found: {audioFilePath}");

                var audioFileInfo = new FileInfo(audioFilePath);
                _logger.LogInformation($"Starting transcription for: {audioFilePath} ({audioFileInfo.Length} bytes)");

                // Convert language name to code if needed
                var languageCode = GetLanguageCode(language);
                _logger.LogInformation($"Using language code: {languageCode} (from: {language})");

                using var form = new MultipartFormDataContent();

                // Read file into memory
                byte[] fileBytes = await File.ReadAllBytesAsync(audioFilePath);
                using var fileContent = new ByteArrayContent(fileBytes);

                // Set proper content type based on file extension
                var extension = Path.GetExtension(audioFilePath).ToLower();
                var contentType = extension switch
                {
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".m4a" => "audio/mp4",
                    ".ogg" => "audio/ogg",
                    _ => "audio/mpeg"
                };

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent("whisper-1"), "model");

                // Use verbose_json for better segment information
                form.Add(new StringContent("verbose_json"), "response_format");

                // Add timestamp granularities for segment-level timestamps
                form.Add(new StringContent("segment"), "timestamp_granularities[]");

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

                // Add temperature for more consistent results
                form.Add(new StringContent("0"), "temperature");

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

                // Process segments for SRT generation
                var segments = new List<SrtEntry>();
                if (whisperResponse?.segments != null)
                {
                    int index = 1;
                    foreach (var segment in whisperResponse.segments)
                    {
                        var segmentText = segment.text?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(segmentText))
                        {
                            var startSeconds = Convert.ToDouble(segment.start ?? 0);
                            var endSeconds = Convert.ToDouble(segment.end ?? startSeconds + 1);

                            segments.Add(new SrtEntry
                            {
                                Index = index++,
                                StartTime = TimeSpan.FromSeconds(startSeconds),
                                EndTime = TimeSpan.FromSeconds(endSeconds),
                                Text = segmentText
                            });
                        }
                    }
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

                _logger.LogInformation($"Created {segments.Count} subtitle segments");

                return new TranscriptionResponse
                {
                    Success = true,
                    Text = fullText,
                    Segments = segments,
                    ProcessingTime = processingTime,
                    Message = $"Transcription completed successfully. Detected language: {detectedLanguage}"
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