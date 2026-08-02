using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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

            // ── Email not confirmed — SignInResult.NotAllowed is returned
            // by PasswordSignInAsync when RequireConfirmedEmail=true and
            // the user's EmailConfirmed flag is still false. Give a
            // specific, actionable message instead of the generic
            // invalid-login error, plus a way to resend the link.
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError("",
                    "আপনার ইমেইল এখনো যাচাই করা হয়নি। " +
                    "যাচাইকরণ ইমেইল আবার পাঠাতে নিচের বাটনে ক্লিক করুন।");
                ViewBag.ShowResend = true;
                ViewBag.UnconfirmedEmail = dto.Email;
                return View(dto);
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

                // ── Send email confirmation instead of signing in
                // immediately. The user can't log in until they click
                // the link (enforced by RequireConfirmedEmail in
                // Program.cs).
                await SendConfirmationEmailAsync(user);

                _logger.LogInformation(
                    "User registered, confirmation email sent: {Email}",
                    dto.Email);

                return RedirectToAction(nameof(RegisterConfirmation),
                    new { email = dto.Email });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(dto);
        }

        // ── "Check your email" page shown right after registration ──
        [HttpGet]
        public IActionResult RegisterConfirmation(string? email)
        {
            ViewBag.Email = email;
            return View();
        }

        // ── Handles the link the user clicks from their email ────────
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return View("ConfirmEmailResult", false);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("ConfirmEmailResult", false);

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                // Already confirmed (e.g. link clicked twice) — treat
                // as success rather than showing a confusing failure.
                return View("ConfirmEmailResult", true);
            }

            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
                _logger.LogInformation(
                    "Email confirmed: {Email}", user.Email);
            else
                _logger.LogWarning(
                    "Email confirmation failed for {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            return View("ConfirmEmailResult", result.Succeeded);
        }

        // ── Resend confirmation email, e.g. from the Login page ──────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            // Always show the same success message whether or not the
            // account exists — don't leak which emails are registered.
            if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                await SendConfirmationEmailAsync(user);
            }

            TempData["Success"] =
                "যদি এই ইমেইলে একটি অ্যাকাউন্ট থাকে, একটি যাচাইকরণ ইমেইল পাঠানো হয়েছে।";
            return RedirectToAction(nameof(Login));
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

        // ── Shared helper — generates a token and sends the email ────
        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

            var confirmationLink = Url.Action(
                nameof(ConfirmEmail), "Account",
                new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme);

            await _email.SendEmailVerificationAsync(
                user.Email!, user.FullName, confirmationLink!);
        }
    }
}