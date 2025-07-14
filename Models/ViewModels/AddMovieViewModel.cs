using System.ComponentModel.DataAnnotations;

namespace Film_website.Models.ViewModels
{
    public class AddMovieViewModel
    {
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
        public int Id { get; set; } 

        // Category selection properties
        public bool IsNewRelease { get; set; }
        public bool IsMostWatched { get; set; }
        public bool IsTopRated { get; set; }
        public bool IsCinema { get; set; }
        public bool IsOngoingTVSeries { get; set; }
        public bool IsFeatureFilm { get; set; }
        public bool IsComingSoon { get; set; }

        // Helper method to get selected categories
        public List<string> GetSelectedCategories()
        {
            var categories = new List<string>();

            if (IsNewRelease) categories.Add("New Releases");
            if (IsMostWatched) categories.Add("Most Watched");
            if (IsTopRated) categories.Add("Top Rated");
            if (IsCinema) categories.Add("Cinema");
            if (IsOngoingTVSeries) categories.Add("Ongoing TV Series");
            if (IsFeatureFilm) categories.Add("Feature Films");
            if (IsComingSoon) categories.Add("Coming Soon");

            return categories;
        }

        // Helper method to set categories from existing movie
        public void SetCategoriesFromMovie(Movie movie)
        {
            var categories = movie.GetCategoriesList();

            IsNewRelease = categories.Contains("New Releases");
            IsMostWatched = categories.Contains("Most Watched");
            IsTopRated = categories.Contains("Top Rated");
            IsCinema = categories.Contains("Cinema");
            IsOngoingTVSeries = categories.Contains("Ongoing TV Series");
            IsFeatureFilm = categories.Contains("Feature Films");
            IsComingSoon = categories.Contains("Coming Soon");
        }

        // Convert to Movie model
        public Movie ToMovie()
        {
            var movie = new Movie
            {
                Title = this.Title,
                Description = this.Description,
                Genre = this.Genre,
                ReleaseYear = this.ReleaseYear
            };

            movie.SetCategoriesFromList(this.GetSelectedCategories());
            return movie;
        }
    }
}