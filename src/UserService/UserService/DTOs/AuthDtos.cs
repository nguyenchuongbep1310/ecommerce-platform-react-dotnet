using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs
{
    /// <summary>
    /// Request DTO for user registration
    /// </summary>
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// Request DTO for user login
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for refreshing tokens
    /// </summary>
    public class RefreshRequestDto
    {
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for token validation
    /// </summary>
    public class ValidateTokenRequestDto
    {
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for authentication operations
    /// </summary>
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }

    /// <summary>
    /// Response DTO for user profile
    /// </summary>
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
    }
}
