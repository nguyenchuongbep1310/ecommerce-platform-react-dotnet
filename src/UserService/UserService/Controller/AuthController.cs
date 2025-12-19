using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using UserService.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.DTOs;
using UserService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            IConfiguration configuration, 
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "User with this email already exists" });
            }

            var user = new ApplicationUser 
            { 
                UserName = model.Email, 
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} registered successfully", model.Email);
                return Ok(new { Message = "User created successfully!" });
            }

            _logger.LogWarning("User registration failed for {Email}: {Errors}", 
                model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return BadRequest(result.Errors);
        }

        /// <summary>
        /// Login and receive access token and refresh token
        /// </summary>
        [HttpPost("login")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                _logger.LogWarning("Failed login attempt for {Email}", model.Email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            
            // Store refresh token with expiry
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in successfully", model.Email);

            return Ok(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email!,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes())
            });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var principal = GetPrincipalFromExpiredToken(model.AccessToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Refresh token request with invalid access token");
                    return BadRequest(new { Message = "Invalid access token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    _logger.LogWarning("Refresh token request for non-existent user {UserId}", userId);
                    return BadRequest(new { Message = "User not found" });
                }

                if (user.RefreshToken != model.RefreshToken)
                {
                    _logger.LogWarning("Refresh token mismatch for user {UserId}", userId);
                    return Unauthorized(new { Message = "Invalid refresh token" });
                }

                if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Expired refresh token for user {UserId}", userId);
                    return Unauthorized(new { Message = "Refresh token has expired" });
                }

                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Tokens refreshed for user {UserId}", userId);

                return Ok(new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Email = user.Email!,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes())
                });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Security token exception during refresh");
                return BadRequest(new { Message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { Message = "An error occurred while refreshing the token" });
            }
        }

        /// <summary>
        /// Revoke refresh token (logout)
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Revoke()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Refresh token revoked for user {UserId}", userId);

            return Ok(new { Message = "Refresh token revoked successfully" });
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                City = user.City,
                State = user.State,
                Country = user.Country,
                ZipCode = user.ZipCode
            });
        }

        /// <summary>
        /// Validate if access token is still valid
        /// </summary>
        [HttpPost("validate")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ValidateToken([FromBody] ValidateTokenRequestDto model)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(model.AccessToken, tokenValidationParameters, out var securityToken);

                if (securityToken is JwtSecurityToken jwtSecurityToken)
                {
                    return Ok(new
                    {
                        Valid = true,
                        ExpiresAt = jwtSecurityToken.ValidTo,
                        UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        Email = principal.FindFirst(ClaimTypes.Email)?.Value
                    });
                }

                return Unauthorized(new { Valid = false, Message = "Invalid token" });
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized(new { Valid = false, Message = "Token has expired" });
            }
            catch (Exception)
            {
                return Unauthorized(new { Valid = false, Message = "Invalid token" });
            }
        }

        #region Private Helper Methods

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes()),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
                ValidateLifetime = false // Don't validate lifetime for expired tokens
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        private double GetTokenLifetimeMinutes()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            return double.Parse(jwtSettings["TokenLifetimeMinutes"] ?? "15");
        }

        #endregion
    }
}