using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.DTOs;
using NewsPortalPro.Models;
using System.Security.Cryptography;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _userManager.Users.Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userDtos.Add(new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    UserName = u.UserName,
                    ProfilePicture = u.ProfilePicture,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / 20);
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(userDtos);
        }

        // FIX: added [ValidateAntiForgeryToken] — previously any page that
        // could get an authenticated Admin's browser to POST here could
        // silently flip a user's active status (CSRF).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            return Ok(new { success = true, isActive = user.IsActive });
        }

        // FIX: added [ValidateAntiForgeryToken] — this endpoint can grant
        // or revoke admin privileges, so CSRF exposure here is a direct
        // privilege-escalation path.
        // FIX: added RoleExistsAsync check — previously a bad/typo'd role
        // string would either throw or silently fail deep inside Identity
        // with no friendly error surfaced to the admin.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Error"] = "অবৈধ রোল নির্বাচন করা হয়েছে";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = $"রোল আপডেট হয়েছে: {role}";
            return RedirectToAction(nameof(Index));
        }

        // FIX: added [ValidateAntiForgeryToken] — this soft-deletes/disables
        // a user account; without CSRF protection any external page could
        // trigger it against an admin's session.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsDeleted = true;
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = "ব্যবহারকারী মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // FIX (two issues):
        // 1. Added [ValidateAntiForgeryToken] — without it, a CSRF request
        //    against a logged-in admin could silently reset any user's
        //    password and take over the account.
        // 2. Removed the hardcoded default password "Admin@12345" — that
        //    value was sitting in source/version control, meaning anyone
        //    with repo access effectively had a skeleton key for every
        //    account that ever got reset without a custom password. Now a
        //    random password is generated per reset and shown to the admin
        //    via TempData so it can be relayed to the user out of band.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string? newPassword = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var passwordToSet = string.IsNullOrWhiteSpace(newPassword)
                ? GenerateSecurePassword()
                : newPassword;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, passwordToSet);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "পাসওয়ার্ড রিসেট হয়েছে";
            // Only shown once, immediately after reset, so the admin can
            // securely relay it to the user — never stored anywhere.
            TempData["GeneratedPassword"] = passwordToSet;
            return RedirectToAction(nameof(Index));
        }

        private static string GenerateSecurePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            Span<byte> buffer = stackalloc byte[12];
            RandomNumberGenerator.Fill(buffer);

            var chars = new char[12];
            chars[0] = upper[buffer[0] % upper.Length];
            chars[1] = lower[buffer[1] % lower.Length];
            chars[2] = digits[buffer[2] % digits.Length];
            chars[3] = special[buffer[3] % special.Length];
            for (int i = 4; i < chars.Length; i++)
                chars[i] = all[buffer[i] % all.Length];

            return new string(chars.OrderBy(_ => Guid.NewGuid()).ToArray());
        }
    }
}