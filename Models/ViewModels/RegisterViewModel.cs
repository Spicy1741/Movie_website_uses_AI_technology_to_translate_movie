using System.ComponentModel.DataAnnotations;

namespace Film_website.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Lastname is required")]
        [StringLength(50, ErrorMessage = "Last name must not exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, ErrorMessage = "Username must be between {2} and {1} characters", MinimumLength = 3)]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Usernames must contain only letters, numbers and underscores")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}