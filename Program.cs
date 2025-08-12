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

// Add after your existing service registrations
builder.Services.AddHttpClient<ITranslationAccuracyService, TranslationAccuracyService>();
builder.Services.AddScoped<ITranslationAccuracyService, TranslationAccuracyService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

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
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".srt"] = "text/plain; charset=utf-8";
provider.Mappings[".vtt"] = "text/vtt; charset=utf-8";
provider.Mappings[".ass"] = "text/plain; charset=utf-8";
provider.Mappings[".ssa"] = "text/plain; charset=utf-8";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = context =>
    {
        var file = context.File;
        var response = context.Context.Response;
        var fileName = file.Name.ToLower();
        // Handle subtitle files
        if (fileName.EndsWith(".srt") || fileName.EndsWith(".vtt") ||
            fileName.EndsWith(".ass") || fileName.EndsWith(".ssa"))
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

            if (fileName.EndsWith(".srt"))
            {
                response.Headers["Content-Type"] = "text/plain; charset=utf-8";
            }
            else if (fileName.EndsWith(".vtt"))
            {
                response.Headers["Content-Type"] = "text/vtt; charset=utf-8";
            }
            else
            {
                response.Headers["Content-Type"] = "text/plain; charset=utf-8";
            }
        }
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.File.Name;
        if (path.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.Add("Content-Type", "text/vtt; charset=utf-8");
            // Add CORS headers if needed
            context.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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