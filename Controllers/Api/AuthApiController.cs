using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsPortalPro.Configurations;
using NewsPortalPro.DTOs;
using NewsPortalPro.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwt;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwt,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt.Value;
            _logger = logger;
        }

        /// <summary>Login and receive JWT token</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                return Unauthorized(ApiResponse<string>.Fail("ইমেইল বা পাসওয়ার্ড সঠিক নয়"));

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return StatusCode(423, ApiResponse<string>.Fail("অ্যাকাউন্ট লক করা হয়েছে"));
                return Unauthorized(ApiResponse<string>.Fail("ইমেইল বা পাসওয়ার্ড সঠিক নয়"));
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);
            var expiry = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

            return Ok(ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = token,
                Expiry = expiry,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    UserName = user.UserName,
                    ProfilePicture = user.ProfilePicture,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                }
            }));
        }

        /// <summary>Register new user</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail(
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(ApiResponse<string>.Fail("পাসওয়ার্ড মিলছে না"));

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(ApiResponse<string>.Fail(
                    result.Errors.Select(e => e.Description).ToList()));

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(ApiResponse<string>.Ok("Registration successful", "নিবন্ধন সফল হয়েছে"));
        }

        /// <summary>Get current user profile</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(ApiResponse<UserDto>.Ok(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                UserName = user.UserName,
                ProfilePicture = user.ProfilePicture,
                Bio = user.Bio,
                Designation = user.Designation,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            }));
        }

        /// <summary>Update profile</summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.Bio = dto.Bio;
            user.Designation = dto.Designation;
            user.FacebookUrl = dto.FacebookUrl;
            user.TwitterUrl = dto.TwitterUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            return Ok(ApiResponse<string>.Ok("Profile updated", "প্রোফাইল আপডেট হয়েছে"));
        }

        /// <summary>Change password</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(ApiResponse<string>.Fail(
                    result.Errors.Select(e => e.Description).ToList()));

            return Ok(ApiResponse<string>.Ok("Password changed", "পাসওয়ার্ড পরিবর্তন হয়েছে"));
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Name, user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}