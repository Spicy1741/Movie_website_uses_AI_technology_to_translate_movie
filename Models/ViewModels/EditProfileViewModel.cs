using System.ComponentModel.DataAnnotations;

namespace Film_website.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(30, MinimumLength = 3)]
        [Display(Name = "Display Username")]
        public string DisplayUserName { get; set; } = string.Empty;

        [Display(Name = "Avatar Image")]
        public IFormFile? AvatarFile { get; set; }

        [Display(Name = "Current Avatar")]
        public string? CurrentAvatarPath { get; set; }

        [Display(Name = "Remove Current Avatar")]
        public bool RemoveAvatar { get; set; } = false;
    }
}