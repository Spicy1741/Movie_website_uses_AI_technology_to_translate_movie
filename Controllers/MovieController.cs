using Microsoft.AspNetCore.Mvc;
using Film_website.Services;
using Film_website.Models;

namespace Film_website.Controllers
{
    public class MovieController : Controller
    {
        private readonly MovieService _movieService;
        private readonly ILogger<MovieController> _logger;

        public MovieController(MovieService movieService, ILogger<MovieController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var movies = await _movieService.GetAllMoviesAsync();
                return View(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movies");
                ViewBag.ErrorMessage = "Unable to load movies at this time.";
                return View(new List<Movie>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                {
                    return NotFound();
                }
                return View(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie details for ID: {MovieId}", id);
                return NotFound();
            }
        }


        [HttpGet]
        public async Task<IActionResult> Search(string query = "")
        {
            ViewBag.SearchQuery = query;

            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.ResultCount = 0;
                return View(new List<Movie>());
            }

            try
            {
                var allMovies = await _movieService.GetAllMoviesAsync();
                var searchResults = allMovies.Where(m =>
                    m.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.Genre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                ViewBag.ResultCount = searchResults.Count;
                return View(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies with query: {Query}", query);
                ViewBag.ErrorMessage = "Unable to search movies at this time.";
                ViewBag.ResultCount = 0;
                return View(new List<Movie>());
            }
        }
    }
}