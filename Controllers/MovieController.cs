using Microsoft.AspNetCore.Mvc;
using Film_website.Services;
using Film_website.Models;
using Film_website.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Film_website.Controllers
{
    public class MovieController : Controller
    {
        private readonly MovieService _movieService;
        private readonly ILogger<MovieController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MovieController(MovieService movieService, ILogger<MovieController> logger, ApplicationDbContext context, UserManager<User> userManager)
        {
            _movieService = movieService;
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // Existing methods
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

        // Updated Details method to include favorite status
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                    return NotFound();

                // Get comments for this movie
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.User)
                    .Include(c => c.CommentLikes)
                    .Where(c => c.MovieId == id && c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                ViewBag.Comments = comments;
                ViewBag.CommentCount = comments.Count + comments.Sum(c => c.Replies.Count);

                // Check if current user has this movie in favorites
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var isFavorite = await _context.Favorites
                        .AnyAsync(f => f.UserId == userId && f.MovieId == id);
                    ViewBag.IsFavorite = isFavorite;
                }
                else
                {
                    ViewBag.IsFavorite = false;
                }

                return View(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie details for ID: {MovieId}", id);
                return NotFound();
            }
        }

        // NEW: Add to Favorites functionality
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorite(int movieId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated." });
                }

                // Check if movie exists
                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                // Check if already in favorites
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

                if (existingFavorite != null)
                {
                    return Json(new { success = false, message = "Movie is already in your favorites." });
                }

                // Add to favorites
                var favorite = new Favorite
                {
                    UserId = userId,
                    MovieId = movieId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Movie added to favorites successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie {MovieId} to favorites for user {UserId}", movieId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { success = false, message = "An error occurred while adding to favorites." });
            }
        }

        // NEW: Remove from Favorites functionality
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorite(int movieId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated." });
                }

                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

                if (favorite == null)
                {
                    return Json(new { success = false, message = "Movie is not in your favorites." });
                }

                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Movie removed from favorites successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing movie {MovieId} from favorites for user {UserId}", movieId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { success = false, message = "An error occurred while removing from favorites." });
            }
        }

        // NEW: Display Favorites page
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Favorites()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var favoriteMovies = await _context.Favorites
                    .Include(f => f.Movie)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => f.Movie)
                    .ToListAsync();

                ViewBag.FavoriteCount = favoriteMovies.Count;
                return View(favoriteMovies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.ErrorMessage = "Unable to load your favorites at this time.";
                return View(new List<Movie>());
            }
        }

        // NEW: Check if movie is in favorites (for AJAX calls)
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> CheckFavoriteStatus(int movieId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { isFavorite = false });
                }

                var isFavorite = await _context.Favorites
                    .AnyAsync(f => f.UserId == userId && f.MovieId == movieId);

                return Json(new { isFavorite = isFavorite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status for movie {MovieId}", movieId);
                return Json(new { isFavorite = false });
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

        // ==================== COMMENT METHODS ====================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int movieId, string content, int? parentCommentId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Comment content cannot be empty" });
                }

                if (content.Length > 1000)
                {
                    return Json(new { success = false, message = "Comment cannot exceed 1000 characters" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Check if movie exists
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }

                // Check if parent comment exists (for replies)
                if (parentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments.FindAsync(parentCommentId.Value);
                    if (parentComment == null || parentComment.MovieId != movieId)
                    {
                        return Json(new { success = false, message = "Parent comment not found" });
                    }
                }

                var comment = new Comment
                {
                    Content = content.Trim(),
                    MovieId = movieId,
                    UserId = userId,
                    ParentCommentId = parentCommentId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Get the user info for the response
                var user = await _userManager.FindByIdAsync(userId);
                var userDisplayName = user?.DisplayUserName ?? "Anonymous";
                var userInitials = GetUserInitials(user?.FirstName, user?.LastName);

                return Json(new
                {
                    success = true,
                    message = "Comment added successfully",
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        createdAt = comment.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                        userDisplayName = userDisplayName,
                        userInitials = userInitials,
                        likeCount = comment.LikeCount,
                        isEdited = comment.IsEdited,
                        parentCommentId = comment.ParentCommentId,
                        canEdit = true,
                        canDelete = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for movie {MovieId}", movieId);
                return Json(new { success = false, message = "An error occurred while adding the comment" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeComment(int commentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var comment = await _context.Comments.FindAsync(commentId);
                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user has already liked this comment
                var existingLike = await _context.CommentLikes
                    .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

                if (existingLike != null)
                {
                    // User has already liked - remove like
                    _context.CommentLikes.Remove(existingLike);
                    comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        liked = false,
                        likeCount = comment.LikeCount,
                        message = "Like removed"
                    });
                }
                else
                {
                    // Add new like
                    var commentLike = new CommentLike
                    {
                        CommentId = commentId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CommentLikes.Add(commentLike);
                    comment.LikeCount++;
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        liked = true,
                        likeCount = comment.LikeCount,
                        message = "Comment liked"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking comment {CommentId}", commentId);
                return Json(new { success = false, message = "An error occurred while processing the like" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string content)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Comment content cannot be empty" });
                }

                if (content.Length > 1000)
                {
                    return Json(new { success = false, message = "Comment cannot exceed 1000 characters" });
                }

                var comment = await _context.Comments.FindAsync(commentId);
                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user owns this comment
                if (comment.UserId != userId)
                {
                    return Json(new { success = false, message = "You can only edit your own comments" });
                }

                comment.Content = content.Trim();
                comment.UpdatedAt = DateTime.UtcNow;
                comment.IsEdited = true;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Comment updated successfully",
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        isEdited = comment.IsEdited,
                        updatedAt = comment.UpdatedAt?.ToString("MMM dd, yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing comment {CommentId}", commentId);
                return Json(new { success = false, message = "An error occurred while editing the comment" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .Include(c => c.CommentLikes)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user owns this comment or is admin
                var isAdmin = User.IsInRole("Admin");
                if (comment.UserId != userId && !isAdmin)
                {
                    return Json(new { success = false, message = "You can only delete your own comments" });
                }

                // Remove all likes for this comment
                _context.CommentLikes.RemoveRange(comment.CommentLikes);

                // Remove the comment (replies will be handled based on your preference)
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return Json(new { success = false, message = "An error occurred while deleting the comment" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int movieId, int page = 1, int pageSize = 10)
        {
            try
            {
                var comments = await GetMovieCommentsAsync(movieId, page, pageSize);
                var totalComments = await _context.Comments
                    .Where(c => c.MovieId == movieId && c.ParentCommentId == null)
                    .CountAsync();

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                // Get user's liked comments
                var userLikedComments = new HashSet<int>();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    userLikedComments = (await _context.CommentLikes
                        .Where(cl => cl.UserId == currentUserId && comments.Select(c => c.Id).Contains(cl.CommentId))
                        .Select(cl => cl.CommentId)
                        .ToListAsync())
                        .ToHashSet();
                }

                return Json(new
                {
                    success = true,
                    comments = comments.Select(c => new
                    {
                        id = c.Id,
                        content = c.Content,
                        createdAt = c.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                        userDisplayName = c.User.DisplayUserName,
                        userInitials = GetUserInitials(c.User.FirstName, c.User.LastName),
                        likeCount = c.LikeCount,
                        isLiked = userLikedComments.Contains(c.Id),
                        isEdited = c.IsEdited,
                        updatedAt = c.UpdatedAt?.ToString("MMM dd, yyyy HH:mm"),
                        repliesCount = c.Replies.Count,
                        canEdit = currentUserId == c.UserId,
                        canDelete = currentUserId == c.UserId || isAdmin
                    }),
                    totalComments = totalComments,
                    currentPage = page,
                    hasNextPage = page * pageSize < totalComments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for movie {MovieId}", movieId);
                return Json(new { success = false, message = "An error occurred while loading comments" });
            }
        }

        // Helper methods
        private async Task<List<Comment>> GetMovieCommentsAsync(int movieId, int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.CommentLikes)
                .Where(c => c.MovieId == movieId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        private string GetUserInitials(string? firstName, string? lastName)
        {
            var initials = "";
            if (!string.IsNullOrEmpty(firstName))
                initials += firstName[0].ToString().ToUpper();
            if (!string.IsNullOrEmpty(lastName))
                initials += lastName[0].ToString().ToUpper();

            return string.IsNullOrEmpty(initials) ? "U" : initials;
        }
    }
}