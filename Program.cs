using Film_website.Data;
using Film_website.Models;
using Film_website.Repositories;
using Film_website.Repositories.Interfaces;
using Film_website.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient for services
builder.Services.AddHttpClient<IWhisperService, WhisperService>();
builder.Services.AddHttpClient<IGptTranslationService, GptTranslationService>();

// Register application services - ENSURE CORRECT MAPPINGS
builder.Services.AddScoped<IWhisperService, WhisperService>();
builder.Services.AddScoped<IGptTranslationService, GptTranslationService>(); // This is the one used in TranslationController
builder.Services.AddScoped<IAudioExtractionService, AudioExtractionService>();
builder.Services.AddScoped<ISrtGeneratorService, SrtGeneratorService>();

// Add after your existing service registrations
builder.Services.AddHttpClient<ITranslationAccuracyService, TranslationAccuracyService>();
builder.Services.AddScoped<ITranslationAccuracyService, TranslationAccuracyService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// *** NEW: Add Session Support ***
builder.Services.AddDistributedMemoryCache(); // Add in-memory caching for sessions
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // Session timeout - 2 hours should be enough for translation results
    options.Cookie.HttpOnly = true; // Security: Session cookie only accessible via HTTP
    options.Cookie.IsEssential = true; // Required for GDPR compliance - session is essential for functionality
    options.Cookie.Name = "FilmWebsite.Session"; // Custom session cookie name
});

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register HttpClient for services
builder.Services.AddHttpClient<IWhisperService, WhisperService>();
builder.Services.AddHttpClient<IGptTranslationService, GptTranslationService>();

// Register application services
builder.Services.AddScoped<IWhisperService, WhisperService>();
builder.Services.AddScoped<IGptTranslationService, GptTranslationService>();
builder.Services.AddScoped<IAudioExtractionService, AudioExtractionService>();
builder.Services.AddScoped<ISrtGeneratorService, SrtGeneratorService>();

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserActivityRepository, UserActivityRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();

// Register services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserActivityService>();
builder.Services.AddScoped<MovieService>();

// Configure file upload size for Whisper AI
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 524288000; // 500MB for large video files
});

// Configure request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 524288000; // 500MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// *** NEW: Add Session Middleware - Must be added before UseAuthentication and UseAuthorization ***
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add role seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Create roles if they don't exist
        string[] roleNames = { "Admin", "User" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles.");
    }
}

// Ensure upload and download directories exist for Whisper AI
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
var downloadsPath = Path.Combine(app.Environment.WebRootPath, "downloads");

Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(downloadsPath);

// Tạo admin user mặc định
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        context.Database.EnsureCreated();

        // Tạo admin user nếu chưa có
        var adminEmail = "admin@filmwebsite.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                DisplayUserName = "admin",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Log admin user creation if activity service is available
                var activityService = services.GetService<UserActivityService>();
                if (activityService != null)
                {
                    await activityService.LogActivityAsync(
                        adminUser.Id,
                        "Register",
                        "Admin user account created during system initialization"
                    );
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tạo database.");
    }
}

app.Run();