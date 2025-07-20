using Film_website.Models;
using Film_website.Models.ViewModels;
using Film_website.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Film_website.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IdentityResult> RegisterUserAsync(RegisterViewModel model)
        {
            // Check if username is already taken
            if (await _userRepository.IsUserNameTakenAsync(model.UserName))
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError
                    {
                        Code = "DuplicateUserName",
                        Description = "Username is already in use."
                    }
                };
                return IdentityResult.Failed(errors.ToArray());
            }

            // Check if email is already taken
            if (await _userRepository.IsEmailTakenAsync(model.Email))
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError
                    {
                        Code = "DuplicateEmail",
                        Description = "Email is already in use."
                    }
                };
                return IdentityResult.Failed(errors.ToArray());
            }

            var user = new User
            {
                UserName = model.Email, // Keep email as the main UserName for Identity
                Email = model.Email,
                DisplayUserName = model.UserName, // Custom username field
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true
            };

            var result = await _userRepository.CreateUserAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Mặc định gán role "User" cho người đăng ký mới
                await _userRepository.AddToRoleAsync(user, "User");
                _logger.LogInformation($"User {model.Email} (username: {model.UserName}) Registration successful");
            }

            return result;
        }

        public async Task<SignInResult> LoginUserAsync(LoginViewModel model)
        {
            var result = await _userRepository.PasswordSignInAsync(model.EmailOrUserName, model.Password, model.RememberMe);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {model.EmailOrUserName} Login successful");
            }

            return result;
        }

        public async Task LogoutUserAsync()
        {
            await _userRepository.SignOutAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.FindByEmailAsync(email);
        }

        public async Task<User?> GetUserByUserNameAsync(string userName)
        {
            return await _userRepository.FindByUserNameAsync(userName);
        }

        public async Task<User?> GetUserByEmailOrUserNameAsync(string emailOrUserName)
        {
            return await _userRepository.FindByEmailOrUserNameAsync(emailOrUserName);
        }

        public async Task<IList<string>> GetUserRolesAsync(User user)
        {
            return await _userRepository.GetRolesAsync(user);
        }

        // Forgot password functionality
        public async Task<bool> ValidateUserForPasswordResetAsync(ForgotPasswordViewModel model)
        {
            var user = await _userRepository.FindByEmailAndUserNameAsync(model.Email, model.UserName);
            return user != null;
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userRepository.FindByEmailAndUserNameAsync(model.Email, model.UserName);

            if (user == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError
                    {
                        Code = "UserNotFound",
                        Description = "No user with this email and username was found."
                    }
                };
                return IdentityResult.Failed(errors.ToArray());
            }

            var result = await _userRepository.ResetPasswordAsync(user, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {model.Email} Password reset successful");
            }

            return result;
        }

        // NEW METHODS: Get all users for admin management
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            return await _userRepository.GetAllUsersWithRolesAsync();
        }

        public async Task<Dictionary<User, IList<string>>> GetAllUsersWithRolesDictionaryAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var usersWithRoles = new Dictionary<User, IList<string>>();

            foreach (var user in users)
            {
                var roles = await _userRepository.GetRolesAsync(user);
                usersWithRoles.Add(user, roles);
            }

            return usersWithRoles;
        }
    }
}