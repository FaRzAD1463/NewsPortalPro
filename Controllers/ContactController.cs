using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Data;
using NewsPortalPro.Models;
using NewsPortalPro.Services;

namespace NewsPortalPro.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        public ContactController(ApplicationDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        public IActionResult Index() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            _db.ContactMessages.Add(new ContactMessage
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Subject = dto.Subject,
                Message = dto.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "আপনার বার্তা পাঠানো হয়েছে। আমরা শীঘ্রই যোগাযোগ করব।";
            return RedirectToAction(nameof(Index));
        }
    }

    public class ContactFormDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Subject { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Required]
        public string Message { get; set; } = string.Empty;
    }
}