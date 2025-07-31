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

        public WhisperService(HttpClient httpClient, IConfiguration configuration, ILogger<WhisperService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<TranscriptionResponse> TranscribeAudioAsync(string audioFilePath, string language = "auto")
        {
            try
            {
                if (!File.Exists(audioFilePath))
                    throw new FileNotFoundException($"Audio file not found: {audioFilePath}");

                _logger.LogInformation($"Starting transcription for: {audioFilePath}");

                using var form = new MultipartFormDataContent();

                byte[] fileBytes = await File.ReadAllBytesAsync(audioFilePath);
                using var fileContent = new ByteArrayContent(fileBytes);

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
                form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
                form.Add(new StringContent("whisper-1"), "model");
                form.Add(new StringContent("verbose_json"), "response_format");
                form.Add(new StringContent("segment"), "timestamp_granularities[]");

                if (language != "auto")
                    form.Add(new StringContent(language), "language");

                _logger.LogInformation("Sending request to OpenAI Whisper API...");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Whisper API error: {response.StatusCode} - {responseContent}");
                    throw new HttpRequestException($"Whisper API error: {response.StatusCode} - {responseContent}");
                }

                _logger.LogInformation("Transcription API call completed successfully");
                var whisperResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                var segments = new List<SrtEntry>();
                if (whisperResponse?.segments != null)
                {
                    int index = 1;
                    foreach (var segment in whisperResponse.segments)
                    {
                        segments.Add(new SrtEntry
                        {
                            Index = index++,
                            StartTime = TimeSpan.FromSeconds((double)(segment.start ?? 0)),
                            EndTime = TimeSpan.FromSeconds((double)(segment.end ?? 0)),
                            Text = segment.text?.ToString()?.Trim() ?? ""
                        });
                    }
                }

                return new TranscriptionResponse
                {
                    Success = true,
                    Text = whisperResponse?.text ?? "",
                    Segments = segments,
                    Message = "Transcription completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Whisper transcription");
                return new TranscriptionResponse
                {
                    Success = false,
                    Message = $"Error during transcription: {ex.Message}"
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

        private string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }
    }
}