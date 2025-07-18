using Film_website.Models;
using Film_website.Models.ViewModels;
using Film_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.IO;

public class AccountController : Controller
{
    private readonly UserService _userService;
    private readonly ILogger<AccountController> _logger;
    private readonly UserManager<User> _userManager;

    public AccountController(UserService userService, ILogger<AccountController> logger, UserManager<User> userManager)
    {
        _userService = userService;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Movie");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.RegisterUserAsync(model);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.LoginUserAsync(model);

        if (result.Succeeded)
        {
            var user = await _userService.GetUserByEmailOrUserNameAsync(model.EmailOrUserName);
            if (user != null)
            {
                var roles = await _userService.GetUserRolesAsync(user);

                // Chuyển hướng dựa trên role
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
        }

        ModelState.AddModelError(string.Empty, "Email/tên người dùng hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _userService.LogoutUserAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userService.GetUserByEmailOrUserNameAsync(User.Identity!.Name!);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // Forgot Password functionality
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Movie");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isValid = await _userService.ValidateUserForPasswordResetAsync(model);

        if (isValid)
        {
            // Store user info in TempData for the next step
            TempData["ResetEmail"] = model.Email;
            TempData["ResetUserName"] = model.UserName;
            TempData["SuccessMessage"] = "Thông tin hợp lệ! Vui lòng nhập mật khẩu mới.";
            return RedirectToAction("ResetPassword");
        }

        ModelState.AddModelError(string.Empty, "Email và tên người dùng không khớp với bất kỳ tài khoản nào.");
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Movie");
        }

        // Check if user came from ForgotPassword
        if (TempData["ResetEmail"] == null || TempData["ResetUserName"] == null)
        {
            return RedirectToAction("ForgotPassword");
        }

        var model = new ResetPasswordViewModel
        {
            Email = TempData["ResetEmail"].ToString()!,
            UserName = TempData["ResetUserName"].ToString()!
        };

        // Keep the data for POST
        TempData.Keep("ResetEmail");
        TempData.Keep("ResetUserName");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Verify the email and username again
        if (TempData["ResetEmail"]?.ToString() != model.Email ||
            TempData["ResetUserName"]?.ToString() != model.UserName)
        {
            ModelState.AddModelError(string.Empty, "Thông tin không hợp lệ. Vui lòng thử lại.");
            return RedirectToAction("ForgotPassword");
        }

        var result = await _userService.ResetPasswordAsync(model);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userService.GetUserByEmailOrUserNameAsync(User.Identity!.Name!);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayUserName = user.DisplayUserName,
            CurrentAvatarPath = user.AvatarPath
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userService.GetUserByEmailOrUserNameAsync(User.Identity!.Name!);
        if (user == null)
        {
            return NotFound();
        }

        // Update user information
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.DisplayUserName = model.DisplayUserName;

        // Handle avatar upload with better error handling
        if (model.AvatarFile != null)
        {
            try
            {
                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Save new avatar
                var avatarPath = await SaveAvatarAsync(model.AvatarFile, user.Id);
                user.AvatarPath = avatarPath;
            }
            catch (InvalidOperationException ex)
            {
                // Add the error to ModelState instead of letting it crash
                ModelState.AddModelError("AvatarFile", ex.Message);
                model.CurrentAvatarPath = user.AvatarPath;
                return View(model);
            }
        }
        else if (model.RemoveAvatar && !string.IsNullOrEmpty(user.AvatarPath))
        {
            // Remove current avatar
            var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarPath.TrimStart('/'));
            if (System.IO.File.Exists(oldAvatarPath))
            {
                System.IO.File.Delete(oldAvatarPath);
            }
            user.AvatarPath = null;
        }

        // Update user in database
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Hồ sơ của bạn đã được cập nhật thành công!";
            return RedirectToAction("Profile");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.CurrentAvatarPath = user.AvatarPath;
            return View(model);
        }
    }

    private async Task<string> SaveAvatarAsync(IFormFile avatarFile, string userId)
    {
        try
        {
            // Validate file type by both extension and MIME type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }; // Added webp
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            var mimeType = avatarFile.ContentType.ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension) || !allowedMimeTypes.Contains(mimeType))
            {
                throw new InvalidOperationException("Only image files (JPG, JPEG, PNG, GIF, WebP) are allowed.");
            }

            // Validate file size (max 5MB)
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size cannot exceed 5MB.");
            }

            // Reset stream position after image validation
            avatarFile.OpenReadStream().Position = 0;

            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            // Return relative path for storage in database
            return $"/uploads/avatars/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving avatar file for user {UserId}", userId);
            throw;
        }
    }
}