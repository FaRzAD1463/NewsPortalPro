using FluentValidation;
using NewsPortalPro.DTOs;

namespace NewsPortalPro.Validators
{
        public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
        {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("মন্তব্য লিখুন")
                .MaximumLength(1000).WithMessage("মন্তব্য সর্বোচ্চ ১০০০ অক্ষর")
                .MinimumLength(3).WithMessage("মন্তব্য কমপক্ষে ৩ অক্ষর");

            RuleFor(x => x.NewsId)
                .GreaterThan(0).WithMessage("সংবাদ আইডি প্রয়োজন");
        }
        }
}