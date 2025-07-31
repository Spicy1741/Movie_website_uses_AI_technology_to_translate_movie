using System.ComponentModel.DataAnnotations;

namespace Film_website.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or username is required")]
        [Display(Name = "Email or username")]
        public string EmailOrUserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember login")]
        public bool RememberMe { get; set; }
    }
}