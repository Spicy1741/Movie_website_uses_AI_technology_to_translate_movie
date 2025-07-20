using Film_website.Models;
using Film_website.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace Film_website.Services
{
    public class MovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly string _uploadPath;

        public MovieService(IMovieRepository movieRepository, IWebHostEnvironment env)
        {
            _movieRepository = movieRepository;
            _uploadPath = Path.Combine(env.WebRootPath, "Uploads/movies");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public async Task<IEnumerable<Movie>> GetAllMoviesAsync()
        {
            return await _movieRepository.GetAllAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(int id)
        {
            return await _movieRepository.GetByIdAsync(id);
        }

        private async Task<string> ConvertSrtToVttAsync(string srtFilePath)
        {
            try
            {
                var srtContent = await File.ReadAllTextAsync(srtFilePath, Encoding.UTF8);
                var vttContent = ConvertSrtContentToVtt(srtContent);
                var vttFilePath = srtFilePath.Replace(".srt", ".vtt");
                await File.WriteAllTextAsync(vttFilePath, vttContent, Encoding.UTF8);
                return vttFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting SRT to VTT: {ex.Message}", ex);
            }
        }

        private string ConvertSrtContentToVtt(string srtContent)
        {
            var lines = srtContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var vttLines = new List<string> { "WEBVTT", "" };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (Regex.IsMatch(line, @"^\d+$"))
                {
                    continue;
                }

                if (line.Contains(" --> ") && line.Contains(","))
                {
                    line = line.Replace(",", ".");
                }

                vttLines.Add(line);
            }

            return string.Join("\n", vttLines);
        }

        public async Task AddMovieAsync(Movie movie, IFormFile movieFile, IFormFile thumbnailFile, IFormFile subtitleFile)
        {
            if (movieFile == null || movieFile.Length == 0)
            {
                throw new ArgumentException("Movie file is required.");
            }

            if (!movieFile.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only MP4 files are allowed.");
            }

            try
            {
                // Handle movie file
                var movieFileName = Guid.NewGuid().ToString() + Path.GetExtension(movieFile.FileName);
                var movieFilePath = Path.Combine(_uploadPath, movieFileName);
                using (var stream = new FileStream(movieFilePath, FileMode.Create))
                {
                    await movieFile.CopyToAsync(stream);
                }
                movie.FilePath = $"/Uploads/movies/{movieFileName}";

                // Handle thumbnail file
                if (thumbnailFile != null && thumbnailFile.Length > 0)
                {
                    var thumbFileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnailFile.FileName);
                    var thumbFilePath = Path.Combine(_uploadPath, thumbFileName);
                    using (var stream = new FileStream(thumbFilePath, FileMode.Create))
                    {
                        await thumbnailFile.CopyToAsync(stream);
                    }
                    movie.ThumbnailPath = $"/Uploads/movies/{thumbFileName}";
                }

                // Handle subtitle file
                if (subtitleFile != null && subtitleFile.Length > 0)
                {
                    var validExtensions = new[] { ".srt", ".vtt" };
                    var fileExtension = Path.GetExtension(subtitleFile.FileName).ToLower();

                    if (!validExtensions.Contains(fileExtension))
                    {
                        throw new ArgumentException("Only SRT and VTT files are allowed for subtitles.");
                    }

                    var subtitleFileName = Guid.NewGuid().ToString() + fileExtension;
                    var subtitleFilePath = Path.Combine(_uploadPath, subtitleFileName);

                    using (var stream = new FileStream(subtitleFilePath, FileMode.Create))
                    {
                        await subtitleFile.CopyToAsync(stream);
                    }

                    if (fileExtension == ".srt")
                    {
                        try
                        {
                            var vttFilePath = await ConvertSrtToVttAsync(subtitleFilePath);
                            var vttFileName = Path.GetFileName(vttFilePath);
                            movie.SubtitlePath = $"/Uploads/movies/{vttFileName}";
                        }
                        catch (Exception ex)
                        {
                            movie.SubtitlePath = $"/Uploads/movies/{subtitleFileName}";
                            Console.WriteLine($"SRT to VTT conversion failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        movie.SubtitlePath = $"/Uploads/movies/{subtitleFileName}";
                    }
                }

                await _movieRepository.AddAsync(movie);
            }
            catch (IOException ex)
            {
                throw new Exception("Error saving files to server.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding movie to database.", ex);
            }
        }

        public async Task UpdateMovieAsync(Movie movie, IFormFile movieFile, IFormFile thumbnailFile, IFormFile subtitleFile)
        {
            var existingMovie = await _movieRepository.GetByIdAsync(movie.Id);
            if (existingMovie == null)
                throw new Exception("Movie not found");

            // Handle movie file
            if (movieFile != null && movieFile.Length > 0)
            {
                if (!movieFile.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Only MP4 files are allowed.");
                }

                if (!string.IsNullOrEmpty(existingMovie.FilePath))
                {
                    var oldFilePath = Path.Combine(_uploadPath, Path.GetFileName(existingMovie.FilePath));
                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                var movieFileName = Guid.NewGuid().ToString() + Path.GetExtension(movieFile.FileName);
                var movieFilePath = Path.Combine(_uploadPath, movieFileName);
                using (var stream = new FileStream(movieFilePath, FileMode.Create))
                {
                    await movieFile.CopyToAsync(stream);
                }
                movie.FilePath = $"/Uploads/movies/{movieFileName}";
            }
            else
            {
                movie.FilePath = existingMovie.FilePath;
            }

            // Handle thumbnail file
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingMovie.ThumbnailPath))
                {
                    var oldThumbPath = Path.Combine(_uploadPath, Path.GetFileName(existingMovie.ThumbnailPath));
                    if (File.Exists(oldThumbPath))
                    {
                        File.Delete(oldThumbPath);
                    }
                }

                var thumbFileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnailFile.FileName);
                var thumbFilePath = Path.Combine(_uploadPath, thumbFileName);
                using (var stream = new FileStream(thumbFilePath, FileMode.Create))
                {
                    await thumbnailFile.CopyToAsync(stream);
                }
                movie.ThumbnailPath = $"/Uploads/movies/{thumbFileName}";
            }
            else
            {
                movie.ThumbnailPath = existingMovie.ThumbnailPath;
            }

            // Handle subtitle file
            if (subtitleFile != null && subtitleFile.Length > 0)
            {
                var validExtensions = new[] { ".srt", ".vtt" };
                var fileExtension = Path.GetExtension(subtitleFile.FileName).ToLower();

                if (!validExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Only SRT and VTT files are allowed for subtitles.");
                }

                if (!string.IsNullOrEmpty(existingMovie.SubtitlePath))
                {
                    var oldSubtitlePath = Path.Combine(_uploadPath, Path.GetFileName(existingMovie.SubtitlePath));
                    if (File.Exists(oldSubtitlePath))
                    {
                        File.Delete(oldSubtitlePath);
                    }

                    var alternateExtension = existingMovie.SubtitlePath.EndsWith(".vtt") ? ".srt" : ".vtt";
                    var alternateFile = oldSubtitlePath.Replace(Path.GetExtension(oldSubtitlePath), alternateExtension);
                    if (File.Exists(alternateFile))
                    {
                        File.Delete(alternateFile);
                    }
                }

                var subtitleFileName = Guid.NewGuid().ToString() + fileExtension;
                var subtitleFilePath = Path.Combine(_uploadPath, subtitleFileName);

                using (var stream = new FileStream(subtitleFilePath, FileMode.Create))
                {
                    await subtitleFile.CopyToAsync(stream);
                }

                if (fileExtension == ".srt")
                {
                    try
                    {
                        var vttFilePath = await ConvertSrtToVttAsync(subtitleFilePath);
                        var vttFileName = Path.GetFileName(vttFilePath);
                        movie.SubtitlePath = $"/Uploads/movies/{vttFileName}";
                    }
                    catch (Exception ex)
                    {
                        movie.SubtitlePath = $"/Uploads/movies/{subtitleFileName}";
                        Console.WriteLine($"SRT to VTT conversion failed: {ex.Message}");
                    }
                }
                else
                {
                    movie.SubtitlePath = $"/Uploads/movies/{subtitleFileName}";
                }
            }
            else
            {
                movie.SubtitlePath = existingMovie.SubtitlePath;
            }

            movie.UpdatedAt = DateTime.UtcNow;
            await _movieRepository.UpdateAsync(movie);
        }

        public async Task DeleteMovieAsync(int id)
        {
            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.FilePath))
                {
                    var filePath = Path.Combine(_uploadPath, Path.GetFileName(movie.FilePath));
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                if (!string.IsNullOrEmpty(movie.ThumbnailPath))
                {
                    var thumbPath = Path.Combine(_uploadPath, Path.GetFileName(movie.ThumbnailPath));
                    if (File.Exists(thumbPath))
                    {
                        File.Delete(thumbPath);
                    }
                }

                if (!string.IsNullOrEmpty(movie.SubtitlePath))
                {
                    var subtitlePath = Path.Combine(_uploadPath, Path.GetFileName(movie.SubtitlePath));
                    if (File.Exists(subtitlePath))
                    {
                        File.Delete(subtitlePath);
                    }

                    var alternateExtension = movie.SubtitlePath.EndsWith(".vtt") ? ".srt" : ".vtt";
                    var alternateFile = subtitlePath.Replace(Path.GetExtension(subtitlePath), alternateExtension);
                    if (File.Exists(alternateFile))
                    {
                        File.Delete(alternateFile);
                    }
                }

                await _movieRepository.DeleteAsync(id);
            }
        }
    }
}