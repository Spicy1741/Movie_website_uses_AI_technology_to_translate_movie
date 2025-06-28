using System.ComponentModel.DataAnnotations;

namespace Film_website.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        [StringLength(50, ErrorMessage = "Genre cannot exceed 50 characters")]
        public string Genre { get; set; }

        [Required(ErrorMessage = "Release Year is required")]
        [Range(1900, 2030, ErrorMessage = "Release Year must be between 1900 and 2030")]
        public int ReleaseYear { get; set; }

        public string? FilePath { get; set; } // Path to movie file
        public string? ThumbnailPath { get; set; } // Path to thumbnail
        public string? SubtitlePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}