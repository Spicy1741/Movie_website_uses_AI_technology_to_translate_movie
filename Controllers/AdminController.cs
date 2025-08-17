using Film_website.Models;
using Film_website.Models.ViewModels;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace Film_website.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserService _userService;
        private readonly UserActivityService? _activityService;
        private readonly ILogger<AdminController> _logger;
        private readonly MovieService _movieService;
        private readonly IWebHostEnvironment _environment;
        private readonly IWhisperService? _whisperService;
        private readonly ITranslationService? _translationService;

        public AdminController(UserService userService,
            ILogger<AdminController> logger,
            MovieService movieService,
            IWebHostEnvironment environment,
            UserActivityService? activityService = null,
            IWhisperService? whisperService = null,
            ITranslationService? translationService = null)
        {
            _userService = userService;
            _logger = logger;
            _movieService = movieService;
            _environment = environment;
            _activityService = activityService;
            _whisperService = whisperService;
            _translationService = translationService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Log admin access if activity service is available
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogAdminAccessAsync(adminUser.Id, "Accessed Admin Index", HttpContext);
                    }
                }

                // Get all users with their roles for user management section
                var usersWithRoles = await _userService.GetAllUsersWithRolesDictionaryAsync();

                ViewBag.Message = "Welcome to AdminPage";
                ViewBag.TotalUsers = usersWithRoles.Count;

                return View(usersWithRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user list");
                ViewBag.ErrorMessage = "An error occurred while loading the user list.";
                ViewBag.Message = "Welcome to the admin page";
                return View(new Dictionary<Film_website.Models.User, IList<string>>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserActivities(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required");
                }

                // If activity service is not available, return empty result
                if (_activityService == null)
                {
                    return Json(new
                    {
                        user = new { id = userId, name = "Unknown", username = "unknown", email = "unknown" },
                        activities = new object[0],
                        totalCount = 0,
                        message = "Activity tracking is not yet configured"
                    });
                }

                // Log admin action
                if (User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogAdminAccessAsync(adminUser.Id, $"Viewed activity log for user {userId}", HttpContext);
                    }
                }

                var activities = await _activityService.GetRecentActivitiesAsync(userId, 50);
                var totalCount = await _activityService.GetTotalUserActivitiesCountAsync(userId);

                // Get user info for display
                var user = await _userService.GetUserByEmailOrUserNameAsync(userId);
                if (user == null)
                {
                    // Try getting by ID directly
                    var allUsers = await _userService.GetAllUsersAsync();
                    user = allUsers.FirstOrDefault(u => u.Id == userId);
                }

                var response = new
                {
                    user = user != null ? new
                    {
                        id = user.Id,
                        name = $"{user.FirstName} {user.LastName}",
                        username = user.DisplayUserName,
                        email = user.Email
                    } : null,
                    activities = activities.Select(a => new {
                        id = a.Id,
                        activityType = a.ActivityType,
                        description = a.Description,
                        createdAt = a.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                        ipAddress = a.IpAddress,
                        location = a.Location ?? "Unknown"
                    }),
                    totalCount = totalCount
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving activities for user {userId}");
                return StatusCode(500, "Error retrieving user activities");
            }
        }

     
        [HttpPost]
        public async Task<IActionResult> UploadSrtFile([FromForm] IFormFile srtFile)
        {
            try
            {
                if (srtFile == null || srtFile.Length == 0)
                {
                    return Json(new { success = false, message = "No SRT file selected" });
                }

                // Validate file type
                var fileExtension = Path.GetExtension(srtFile.FileName).ToLower();
                if (fileExtension != ".srt")
                {
                    return Json(new { success = false, message = "Please upload a valid .srt file" });
                }

                // Read SRT content
                using var reader = new StreamReader(srtFile.OpenReadStream());
                var srtContent = await reader.ReadToEndAsync();

                // Log activity
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogActivityAsync(
                            adminUser.Id,
                            "SRT Upload",
                            $"Uploaded SRT file: {srtFile.FileName}"
                        );
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "SRT file uploaded successfully",
                    fileName = srtFile.FileName,
                    content = srtContent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading SRT file");
                return Json(new { success = false, message = "Error uploading SRT file" });
            }
        }


        // MOVIE MANAGEMENT METHODS
        public async Task<IActionResult> ManageMovies()
        {
            try
            {
                var movies = await _movieService.GetAllMoviesAsync();
                return View(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movies");
                TempData["Error"] = "Error loading movies.";
                return View(new List<Movie>());
            }
        }

        [HttpGet]
        public IActionResult AddMovie()
        {
            return View(new AddMovieViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMovie(AddMovieViewModel viewModel, IFormFile movieFile, IFormFile thumbnailFile, IFormFile subtitleFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Convert ViewModel to Movie entity
                    var movie = viewModel.ToMovie();

                    // Add the movie using the service
                    await _movieService.AddMovieAsync(movie, movieFile, thumbnailFile, subtitleFile);

                    TempData["Success"] = "Movie added successfully with selected categories!";
                    return RedirectToAction("ManageMovies");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding movie");
                    TempData["Error"] = $"Error adding movie: {ex.Message}";
                    return View(viewModel);
                }
            }
            return View(viewModel);
        }

        // Also update the EditMovie methods to handle categories
        [HttpGet]
        public async Task<IActionResult> EditMovie(int id)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                    return NotFound();

                // Convert Movie to ViewModel for editing
                var viewModel = new AddMovieViewModel
                {
                    Title = movie.Title,
                    Description = movie.Description,
                    Genre = movie.Genre,
                    ReleaseYear = movie.ReleaseYear
                };

                // Set categories from existing movie
                viewModel.SetCategoriesFromMovie(movie);

                return View("EditMovie", viewModel); // You'll need to create EditMovie.cshtml similar to AddMovie
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie for edit");
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(int id, AddMovieViewModel viewModel, IFormFile movieFile, IFormFile thumbnailFile, IFormFile subtitleFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the existing movie
                    var existingMovie = await _movieService.GetMovieByIdAsync(id);
                    if (existingMovie == null)
                        return NotFound();

                    // Update the movie properties
                    existingMovie.Title = viewModel.Title;
                    existingMovie.Description = viewModel.Description;
                    existingMovie.Genre = viewModel.Genre;
                    existingMovie.ReleaseYear = viewModel.ReleaseYear;

                    // Update categories
                    existingMovie.SetCategoriesFromList(viewModel.GetSelectedCategories());
                    existingMovie.UpdatedAt = DateTime.UtcNow;

                    await _movieService.UpdateMovieAsync(existingMovie, movieFile, thumbnailFile, subtitleFile);
                    TempData["Success"] = "Movie updated successfully with categories!";
                    return RedirectToAction("ManageMovies");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating movie");
                    TempData["Error"] = $"Error updating movie: {ex.Message}";
                    return View(viewModel);
                }
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                await _movieService.DeleteMovieAsync(id);
                TempData["Success"] = "Movie deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie");
                TempData["Error"] = "Error deleting movie.";
            }
            return RedirectToAction("ManageMovies");
        }

        public async Task<IActionResult> ViewMovie(int id)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                    return NotFound();
                return View(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie details");
                return NotFound();
            }
        }

        // DASHBOARD METHODS
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Log admin access
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogAdminAccessAsync(adminUser.Id, "Accessed Dashboard", HttpContext);
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Error loading dashboard.";
                return RedirectToAction("Index");
            }
        }

        // USER MANAGEMENT METHODS
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                // Log admin access
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogAdminAccessAsync(adminUser.Id, "Accessed User Management", HttpContext);
                    }
                }

                var usersWithRoles = await _userService.GetAllUsersWithRolesDictionaryAsync();
                return View(usersWithRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management");
                TempData["Error"] = "Error loading user management.";
                return RedirectToAction("Index");
            }
        }

        // SYSTEM SETTINGS
        public async Task<IActionResult> SystemSettings()
        {
            try
            {
                // Log admin access
                if (_activityService != null && User.Identity?.Name != null)
                {
                    var adminUser = await _userService.GetUserByEmailOrUserNameAsync(User.Identity.Name);
                    if (adminUser != null)
                    {
                        await _activityService.LogAdminAccessAsync(adminUser.Id, "Accessed System Settings", HttpContext);
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing system settings");
                TempData["Error"] = "Error loading system settings.";
                return RedirectToAction("Index");
            }
        }

        // Error handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}