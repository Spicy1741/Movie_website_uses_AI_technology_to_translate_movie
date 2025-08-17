using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Film_website.Models;

namespace Film_website.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Favorite> Favorites { get; set; } // Add this line

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Existing User configuration
            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DisplayUserName).IsRequired().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.DisplayUserName).IsUnique();
            });

            // Existing UserActivity configuration
            builder.Entity<UserActivity>(entity =>
            {
                entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ActivityType);
            });

            // FIXED Comment configuration
            builder.Entity<Comment>(entity =>
            {
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Foreign key relationships
                entity.HasOne(e => e.Movie)
                      .WithMany()
                      .HasForeignKey(e => e.MovieId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing for replies
                entity.HasOne(e => e.ParentComment)
                      .WithMany(e => e.Replies)
                      .HasForeignKey(e => e.ParentCommentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes for better performance
                entity.HasIndex(e => e.MovieId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ParentCommentId);
            });

            // FIXED CommentLike configuration - This is the key fix!
            builder.Entity<CommentLike>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Foreign key relationships - FIXED CASCADE ISSUE
                entity.HasOne(e => e.Comment)
                      .WithMany(e => e.CommentLikes)
                      .HasForeignKey(e => e.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // CHANGED: User relationship to NoAction to prevent cascade cycles
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Ensure a user can only like a comment once
                entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();

                // Indexes for better performance
                entity.HasIndex(e => e.CommentId);
                entity.HasIndex(e => e.UserId);
            });

            // NEW: Favorite configuration
            builder.Entity<Favorite>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Movie)
                      .WithMany()
                      .HasForeignKey(e => e.MovieId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Ensure a user can only favorite a movie once
                entity.HasIndex(e => new { e.UserId, e.MovieId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.MovieId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Existing role seeding
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = "User",
                    NormalizedName = "USER"
                }
            );
        }
    }
}