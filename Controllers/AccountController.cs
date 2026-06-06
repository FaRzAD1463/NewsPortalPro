using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsPortalPro.Configurations;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
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
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwt,
            IEmailService email,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt.Value;
            _email = email;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginDto dto, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("",
                    "ইমেইল বা পাসওয়ার্ড সঠিক নয়");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, dto.Password, dto.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User logged in: {Email}", dto.Email);
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("",
                    "অ্যাকাউন্ট লক করা হয়েছে। ১৫ মিনিট পরে চেষ্টা করুন।");
            else
                ModelState.AddModelError("",
                    "ইমেইল বা পাসওয়ার্ড সঠিক নয়");

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

        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> UpdateProfile(
    string FullName, string? Designation,
    string? Bio, string? FacebookUrl, string? TwitterUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FullName = FullName;
            user.Designation = Designation;
            user.Bio = Bio;
            user.FacebookUrl = FacebookUrl;
            user.TwitterUrl = TwitterUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "প্রোফাইল আপডেট হয়েছে";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> ChangePassword(
            string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                TempData["Error"] = "পাসওয়ার্ড মিলছে না";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(
                user, CurrentPassword, NewPassword);

            if (result.Succeeded)
                TempData["Success"] = "পাসওয়ার্ড পরিবর্তন হয়েছে";
            else
                TempData["Error"] = string.Join(", ",
                    result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Profile));
        }

        // Add this to Controllers/AccountController.cs

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}