using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsletterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        public NewsletterController(ApplicationDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            var newsletters = await _db.Newsletters
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            ViewBag.SubscriberCount = await _db.Subscribers.CountAsync(s => s.IsActive);
            return View(newsletters);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string subject, string body)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var newsletter = new Newsletter
            {
                Subject = subject,
                Body = body,
                CreatedById = userId
            };
            _db.Newsletters.Add(newsletter);
            await _db.SaveChangesAsync();
            TempData["Success"] = "নিউজলেটার তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // FIX: added [ValidateAntiForgeryToken] — this triggers an actual
        // mass email send to every subscriber. A CSRF request against this
        // endpoint could blast an unintended newsletter to your entire
        // list, so this was the highest-impact missing token in this file.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            var newsletter = await _db.Newsletters.FindAsync(id);
            if (newsletter == null) return NotFound();
            if (newsletter.IsSent)
            {
                TempData["Error"] = "ইতিমধ্যে পাঠানো হয়েছে";
                return RedirectToAction(nameof(Index));
            }

            var subscribers = await _db.Subscribers
                .Where(s => s.IsActive && s.IsConfirmed)
                .Select(s => s.Email)
                .ToListAsync();

            await _email.SendNewsletterAsync(newsletter, subscribers);

            newsletter.IsSent = true;
            newsletter.SentAt = DateTime.UtcNow;
            newsletter.RecipientCount = subscribers.Count;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{subscribers.Count} জন সাবস্ক্রাইবারকে পাঠানো হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var newsletter = await _db.Newsletters.FindAsync(id);
            if (newsletter == null) return NotFound();
            _db.Newsletters.Remove(newsletter);
            await _db.SaveChangesAsync();
            TempData["Success"] = "নিউজলেটার মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }
    }
}