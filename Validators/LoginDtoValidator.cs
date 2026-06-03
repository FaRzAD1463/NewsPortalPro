using FluentValidation;
using NewsPortalPro.DTOs;

namespace NewsPortalPro.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("ইমেইল প্রয়োজন")
                .EmailAddress().WithMessage("সঠিক ইমেইল দিন");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("পাসওয়ার্ড প্রয়োজন");
        }
    }
}