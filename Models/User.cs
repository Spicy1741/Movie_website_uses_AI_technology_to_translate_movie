using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Film_website.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(30, MinimumLength = 3)]
        public string DisplayUserName { get; set; } = string.Empty; // Custom username field

        // Add Avatar property
        [StringLength(255)]
        public string? AvatarPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}