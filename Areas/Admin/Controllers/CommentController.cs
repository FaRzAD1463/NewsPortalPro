using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;

namespace NewsPortalPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class CommentController : Controller
    {
        private readonly ICommentService _comments;

        public CommentController(ICommentService comments) => _comments = comments;

        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _comments.GetPendingAsync(page, 20);
            return View(result);
        }

        // FIX: added [ValidateAntiForgeryToken] — moderation actions
        // (approve/reject/delete) were CSRF-exploitable against an active
        // Admin/Editor session.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await _comments.ApproveAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            await _comments.RejectAsync(id);
            return Ok(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _comments.DeleteAsync(id);
            return Ok(new { success = true });
        }
    }
}