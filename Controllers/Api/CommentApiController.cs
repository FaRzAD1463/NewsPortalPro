using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
using System.Security.Claims;

namespace NewsPortalPro.Controllers.Api
{
    [ApiController]
    [Route("api/comments")]
    [Produces("application/json")]
    public class CommentApiController : ControllerBase
    {
        private readonly ICommentService _comments;

        public CommentApiController(ICommentService comments) => _comments = comments;

        [HttpGet("{newsId:int}")]
        public async Task<IActionResult> GetByNews(int newsId)
        {
            var result = await _comments.GetByNewsIdAsync(newsId);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var id = await _comments.AddAsync(dto, userId, ip);

            return Ok(ApiResponse<object>.Ok(new { id }, "মন্তব্য জমা দেওয়া হয়েছে"));
        }

        // FIX: was [Authorize] only — any logged-in user could delete any
        // comment by id. Restricted to Admin/Editor, same as Approve/Reject
        // below. If you also want the comment's own author to be able to
        // delete it, ICommentService needs a way to fetch the comment's
        // UserId first (e.g. GetByIdAsync) so we can compare it here.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _comments.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(ApiResponse<string>.Ok("Deleted"));
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _comments.ApproveAsync(id);
            if (!result) return NotFound();
            return Ok(ApiResponse<string>.Ok("Approved"));
        }

        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _comments.RejectAsync(id);
            if (!result) return NotFound();
            return Ok(ApiResponse<string>.Ok("Rejected"));
        }
    }
}