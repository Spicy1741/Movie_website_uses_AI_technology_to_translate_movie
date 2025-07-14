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

        // New property for categories - stored as comma-separated string
        [StringLength(500, ErrorMessage = "Categories cannot exceed 500 characters")]
        public string? Categories { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Helper method to get categories as a list
        public List<string> GetCategoriesList()
        {
            if (string.IsNullOrEmpty(Categories))
                return new List<string>();

            return Categories.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(c => c.Trim())
                           .ToList();
        }

        // Helper method to set categories from a list
        public void SetCategoriesFromList(List<string> categories)
        {
            Categories = categories != null && categories.Any()
                ? string.Join(",", categories.Where(c => !string.IsNullOrWhiteSpace(c)))
                : null;
        }
    }
}