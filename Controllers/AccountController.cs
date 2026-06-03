using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsPortalPro.Configurations;
using NewsPortalPro.DTOs;
using NewsPortalPro.Models;
using NewsPortalPro.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NewsPortalPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwt;
        private readonly IEmailService _email;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwt,
            IEmailService email)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt.Value;
            _email = email;
        }

        [HttpGet] public IActionResult Login() => View();
        [HttpGet] public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "ইমেইল বা পাসওয়ার্ড সঠিক নয়");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, dto.Password, dto.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "অ্যাকাউন্ট লক করা হয়েছে। ১৫ মিনিট পরে চেষ্টা করুন।");
            else
                ModelState.AddModelError("", "ইমেইল বা পাসওয়ার্ড সঠিক নয়");

            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            if (dto.Password != dto.ConfirmPassword)
            {
                ModelState.AddModelError("", "পাসওয়ার্ড মিলছে না");
                return View(dto);
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet] public IActionResult AccessDenied() => View();
    }
}