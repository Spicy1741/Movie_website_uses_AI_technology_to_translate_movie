namespace Film_website.Models
{
    public class HomePageViewModel
    {
        public Movie? FeaturedMovie { get; set; }
        public int TotalMoviesCount { get; set; }

        // Dictionary to store movies by category
        public Dictionary<string, List<Movie>> MoviesByCategory { get; set; } = new Dictionary<string, List<Movie>>();

        // List of available categories (only those that have movies)
        public List<string> AvailableCategories { get; set; } = new List<string>();

        // Helper method to get movies for a specific category
        public List<Movie> GetMoviesForCategory(string categoryName)
        {
            return MoviesByCategory.ContainsKey(categoryName)
                ? MoviesByCategory[categoryName]
                : new List<Movie>();
        }

        // Helper method to check if a category has movies
        public bool HasMoviesInCategory(string categoryName)
        {
            return MoviesByCategory.ContainsKey(categoryName) && MoviesByCategory[categoryName].Any();
        }

        // Method to get category display name with emoji
        public string GetCategoryDisplayName(string categoryName)
        {
            return categoryName switch
            {
                "New Releases" => "🆕 New Releases",
                "Most Watched" => "🔥 Most Watched",
                "Top Rated" => "⭐ Top Rated",
                "Cinema" => "🎭 Cinema",
                "Ongoing TV Series" => "📺 Ongoing TV Series",
                "Feature Films" => "🎬 Feature Films",
                "Coming Soon" => "🚀 Coming Soon",
                _ => categoryName
            };
        }

        // Get category priority for ordering (lower number = higher priority)
        public int GetCategoryPriority(string categoryName)
        {
            return categoryName switch
            {
                "New Releases" => 1,
                "Most Watched" => 2,
                "Top Rated" => 3,
                "Feature Films" => 4,
                "Cinema" => 5,
                "Ongoing TV Series" => 6,
                "Coming Soon" => 7,
                _ => 99
            };
        }
    }
}