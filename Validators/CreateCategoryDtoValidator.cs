using FluentValidation;
using NewsPortalPro.DTOs;

namespace NewsPortalPro.Validators
{
    public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("বিভাগের নাম প্রয়োজন")
                .MaximumLength(100).WithMessage("নাম সর্বোচ্চ ১০০ অক্ষর");

            RuleFor(x => x.ColorCode)
                .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("সঠিক রঙ কোড দিন (যেমন: #e74c3c)")
                .When(x => !string.IsNullOrEmpty(x.ColorCode));

            RuleFor(x => x.MetaTitle)
                .MaximumLength(160)
                .When(x => x.MetaTitle != null);

            RuleFor(x => x.MetaDescription)
                .MaximumLength(300)
                .When(x => x.MetaDescription != null);
        }
    }
}