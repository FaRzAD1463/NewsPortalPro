using FluentValidation;
using NewsPortalPro.DTOs;

namespace NewsPortalPro.Validators
{
        public class RegisterDtoValidator : AbstractValidator<RegisterDto>
        {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("নাম প্রয়োজন")
                .MaximumLength(100).WithMessage("নাম সর্বোচ্চ ১০০ অক্ষর");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("ইমেইল প্রয়োজন")
                .EmailAddress().WithMessage("সঠিক ইমেইল ঠিকানা দিন");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("পাসওয়ার্ড প্রয়োজন")
                .MinimumLength(8).WithMessage("পাসওয়ার্ড কমপক্ষে ৮ অক্ষর")
                .Matches("[0-9]").WithMessage("পাসওয়ার্ডে কমপক্ষে একটি সংখ্যা থাকতে হবে");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("পাসওয়ার্ড মিলছে না");
        }
        }
}