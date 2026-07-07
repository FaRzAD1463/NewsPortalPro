using FluentValidation;
using NewsPortalPro.DTOs;

namespace NewsPortalPro.Validators
{
        public class CreateNewsDtoValidator : AbstractValidator<CreateNewsDto>
        {
        public CreateNewsDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("শিরোনাম প্রয়োজন")
                .MaximumLength(300).WithMessage("শিরোনাম সর্বোচ্চ ৩০০ অক্ষর");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("বিষয়বস্তু প্রয়োজন")
                .MinimumLength(50).WithMessage("বিষয়বস্তু কমপক্ষে ৫০ অক্ষর হতে হবে");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("বিভাগ নির্বাচন করুন");

            RuleFor(x => x.Summary)
                .MaximumLength(500).WithMessage("সারসংক্ষেপ সর্বোচ্চ ৫০০ অক্ষর")
                .When(x => x.Summary != null);

            RuleFor(x => x.MetaTitle)
                .MaximumLength(160).WithMessage("Meta শিরোনাম সর্বোচ্চ ১৬০ অক্ষর")
                .When(x => x.MetaTitle != null);

            RuleFor(x => x.MetaDescription)
                .MaximumLength(300).WithMessage("Meta বিবরণ সর্বোচ্চ ৩০০ অক্ষর")
                .When(x => x.MetaDescription != null);

            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("শিডিউলের তারিখ ভবিষ্যতে হতে হবে")
                .When(x => x.ScheduledAt.HasValue);
        }
        }

        public class UpdateNewsDtoValidator : AbstractValidator<UpdateNewsDto>
        {
        public UpdateNewsDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            Include(new CreateNewsDtoValidator());
        }
        }
}