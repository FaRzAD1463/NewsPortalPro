using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsPortalPro.Data;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;
using System.Security.Claims;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsletterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        public NewsletterController(
            ApplicationDbContext db,
            IEmailService email)
        {
            _db = db;
            _email = email;
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var newsletters = await _db.Newsletters
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.SubscriberCount = await _db.Subscribers
                .CountAsync(s => s.IsActive && s.IsConfirmed);

            return View(newsletters);
        }

        // ── Create GET ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Create() => View();

        // ── Create POST ────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "বিষয় এবং বিষয়বস্তু প্রয়োজন";
                return View();
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var newsletter = new Newsletter
            {
                Subject = subject.Trim(),
                Body = body,
                CreatedById = userId
            };

            _db.Newsletters.Add(newsletter);
            await _db.SaveChangesAsync();

            TempData["Success"] = "নিউজলেটার তৈরি হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Send ───────────────────────────────────────────────
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

            // Only send to confirmed active subscribers
            var subscribers = await _db.Subscribers
                .Where(s => s.IsActive && s.IsConfirmed)
                .ToListAsync();

            if (!subscribers.Any())
            {
                TempData["Error"] =
                    "কোনো সক্রিয় সাবস্ক্রাইবার নেই";
                return RedirectToAction(nameof(Index));
            }

            // Build base URL for unsubscribe links
            var baseUrl =
                $"{Request.Scheme}://{Request.Host}";

            // Add unsubscribe link to email body for each subscriber
            var sentCount = 0;
            var errors = 0;

            foreach (var subscriber in subscribers)
            {
                try
                {
                    var unsubLink =
                        $"{baseUrl}/api/newsletter/" +
                        $"unsubscribe-token?" +
                        $"token={subscriber.ConfirmationToken}";

                    var bodyWithUnsub = newsletter.Body +
                        $"<br/><br/><hr/>" +
                        $"<p style='font-size:12px;color:#888'>" +
                        $"আপনি এই নিউজলেটার আর পেতে না চাইলে " +
                        $"<a href='{unsubLink}'>এখানে ক্লিক করুন</a>" +
                        $"</p>";

                    var newsletterCopy = new Newsletter
                    {
                        Subject = newsletter.Subject,
                        Body = bodyWithUnsub
                    };

                    await _email.SendNewsletterAsync(
                        newsletterCopy,
                        new List<string> { subscriber.Email });

                    sentCount++;
                }
                catch
                {
                    errors++;
                }
            }

            newsletter.IsSent = true;
            newsletter.SentAt = DateTime.UtcNow;
            newsletter.RecipientCount = sentCount;
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"{sentCount} জন সাবস্ক্রাইবারকে পাঠানো হয়েছে" +
                (errors > 0 ? $" ({errors} টি ব্যর্থ)" : "");

            return RedirectToAction(nameof(Index));
        }

        // ── Preview ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var newsletter = await _db.Newsletters.FindAsync(id);
            if (newsletter == null) return NotFound();
            return Content(newsletter.Body, "text/html");
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var newsletter = await _db.Newsletters.FindAsync(id);
            if (newsletter == null) return NotFound();

            if (newsletter.IsSent)
            {
                TempData["Error"] =
                    "পাঠানো নিউজলেটার মুছে ফেলা যাবে না";
                return RedirectToAction(nameof(Index));
            }

            _db.Newsletters.Remove(newsletter);
            await _db.SaveChangesAsync();
            TempData["Success"] = "নিউজলেটার মুছে ফেলা হয়েছে";
            return RedirectToAction(nameof(Index));
        }

        // ── Subscribers list ───────────────────────────────────
        public async Task<IActionResult> Subscribers(
            int page = 1)
        {
            const int pageSize = 50;
            var total = await _db.Subscribers.CountAsync();
            var subs = await _db.Subscribers
                .OrderByDescending(s => s.SubscribedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;

            return View(subs);
        }

        // ── Remove subscriber ──────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSubscriber(int id)
        {
            var sub = await _db.Subscribers.FindAsync(id);
            if (sub != null)
            {
                _db.Subscribers.Remove(sub);
                await _db.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }
    }
}