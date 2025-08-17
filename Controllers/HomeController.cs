using System.Diagnostics;
using Film_website.Models;
using Film_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace Film_website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MovieService _movieService;

        public HomeController(ILogger<HomeController> logger, MovieService movieService)
        {
            _logger = logger;
            _movieService = movieService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all movies from database
                var allMovies = (await _movieService.GetAllMoviesAsync()).ToList();

                // Create a view model with categorized movies
                var viewModel = new HomePageViewModel
                {
                    // Get a featured movie for the hero section (latest movie)
                    FeaturedMovie = allMovies
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault(),

                    // Total count for statistics
                    TotalMoviesCount = allMovies.Count
                };

                // Organize movies by their assigned categories
                var moviesByCategory = new Dictionary<string, List<Movie>>();
                var availableCategories = new HashSet<string>();

                foreach (var movie in allMovies)
                {
                    var categories = movie.GetCategoriesList();

                    if (categories.Any())
                    {
                        foreach (var category in categories)
                        {
                            if (!moviesByCategory.ContainsKey(category))
                            {
                                moviesByCategory[category] = new List<Movie>();
                            }

                            moviesByCategory[category].Add(movie);
                            availableCategories.Add(category);
                        }
                    }
                    else
                    {
                        // For movies without categories, add them to "Feature Films" as default
                        const string defaultCategory = "Feature Films";
                        if (!moviesByCategory.ContainsKey(defaultCategory))
                        {
                            moviesByCategory[defaultCategory] = new List<Movie>();
                        }
                        moviesByCategory[defaultCategory].Add(movie);
                        availableCategories.Add(defaultCategory);
                    }
                }

                // Limit movies per category to 6 for better performance and UI
                foreach (var category in moviesByCategory.Keys.ToList())
                {
                    moviesByCategory[category] = moviesByCategory[category]
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(6)
                        .ToList();
                }

                // Sort categories by priority
                var sortedCategories = availableCategories
                    .OrderBy(cat => viewModel.GetCategoryPriority(cat))
                    .ToList();

                viewModel.MoviesByCategory = moviesByCategory;
                viewModel.AvailableCategories = sortedCategories;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage data");

                // Return empty view model in case of error
                var emptyViewModel = new HomePageViewModel
                {
                    MoviesByCategory = new Dictionary<string, List<Movie>>(),
                    AvailableCategories = new List<string>(),
                    FeaturedMovie = null,
                    TotalMoviesCount = 0
                };

                ViewBag.ErrorMessage = "Unable to load movies at this time.";
                return View(emptyViewModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}