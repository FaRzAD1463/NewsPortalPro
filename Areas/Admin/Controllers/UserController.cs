using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.DTOs;
using NewsPortalPro.Models;

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

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            return Ok(new { success = true, isActive = user.IsActive });
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = $"রোল আপডেট হয়েছে: {role}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
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

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, string newPassword = "Admin@12345")
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, newPassword);
            TempData["Success"] = "পাসওয়ার্ড রিসেট হয়েছে";
            return RedirectToAction(nameof(Index));
        }
    }
}