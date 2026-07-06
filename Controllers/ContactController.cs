using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Data;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            ApplicationDbContext db,
            IEmailService email,
            ILogger<ContactController> logger)
        {
            _db = db;
            _email = email;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var message = new ContactMessage
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Subject = dto.Subject,
                Message = dto.Message,
                IpAddress = HttpContext.Connection
                    .RemoteIpAddress?.ToString()
            };

            _db.ContactMessages.Add(message);
            await _db.SaveChangesAsync();

            // Notify admin
            try
            {
                // FIX: dto.Name / dto.Email / dto.Message were being
                // interpolated directly into HTML with no encoding —
                // a submitted <script>/<img onerror> payload would
                // render in the admin's email client. HTML-encoded below.
                var safeName = System.Net.WebUtility.HtmlEncode(dto.Name);
                var safeEmail = System.Net.WebUtility.HtmlEncode(dto.Email);
                var safeMessage = System.Net.WebUtility.HtmlEncode(dto.Message);

                await _email.SendAsync(
                    "admin@newsportalpro.com",
                    $"নতুন যোগাযোগ: {dto.Subject}",
                    $"<p><strong>নাম:</strong> {safeName}</p>" +
                    $"<p><strong>ইমেইল:</strong> {safeEmail}</p>" +
                    $"<p><strong>বার্তা:</strong> {safeMessage}</p>"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send contact notification");
            }

            TempData["Success"] =
                "আপনার বার্তা পাঠানো হয়েছে। আমরা শীঘ্রই যোগাযোগ করব।";
            return RedirectToAction(nameof(Index));
        }
    }
}