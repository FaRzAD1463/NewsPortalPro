using System.ComponentModel.DataAnnotations;

namespace NewsPortalPro.DTOs
{
    public class ContactFormDto
    {
        [Required(ErrorMessage = "নাম প্রয়োজন")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "ইমেইল প্রয়োজন")]
        [EmailAddress(ErrorMessage = "সঠিক ইমেইল দিন")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "বিষয় প্রয়োজন")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "বার্তা প্রয়োজন")]
        public string Message { get; set; } = string.Empty;
    }
}