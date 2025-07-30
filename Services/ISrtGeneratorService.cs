using Film_website.Models;

namespace Film_website.Services
{
    public interface ISrtGeneratorService
    {
        string GenerateSrt(List<SrtEntry> entries);
        List<SrtEntry> ParseSrt(string srtContent);
        string FormatTime(TimeSpan time);
        TimeSpan ParseTime(string timeString);
    }
}