using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NewsPortalPro.Configurations;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Models;

namespace NewsPortalPro.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var mail = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mail.To.Add(to);
                await client.SendMailAsync(mail);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
            }
        }

        public async Task SendEmailVerificationAsync(string email, string userName, string verificationLink)
        {
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto'>
                    <h2>ইমেইল যাচাই করুন</h2>
                    <p>প্রিয় {userName},</p>
                    <p>আপনার অ্যাকাউন্ট যাচাই করতে নিচের বাটনে ক্লিক করুন:</p>
                    <a href='{verificationLink}'
                       style='background:#e74c3c;color:white;padding:12px 24px;
                              text-decoration:none;border-radius:4px;display:inline-block'>
                        ইমেইল যাচাই করুন
                    </a>
                    <p style='color:#999;font-size:12px;margin-top:20px'>
                        এই লিঙ্কটি ২৪ ঘন্টার মধ্যে মেয়াদ শেষ হবে।
                    </p>
                </div>";

            await SendAsync(email, "ইমেইল যাচাই করুন - নিউজপোর্টাল প্রো", body);
        }

        public async Task SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto'>
                    <h2>পাসওয়ার্ড রিসেট</h2>
                    <p>প্রিয় {userName},</p>
                    <p>আপনার পাসওয়ার্ড রিসেট করতে নিচের বাটনে ক্লিক করুন:</p>
                    <a href='{resetLink}'
                       style='background:#3498db;color:white;padding:12px 24px;
                              text-decoration:none;border-radius:4px;display:inline-block'>
                        পাসওয়ার্ড রিসেট করুন
                    </a>
                    <p style='color:#999;font-size:12px;margin-top:20px'>
                        এই লিঙ্কটি ১ ঘন্টার মধ্যে মেয়াদ শেষ হবে।
                    </p>
                </div>";

            await SendAsync(email, "পাসওয়ার্ড রিসেট - নিউজপোর্টাল প্রো", body);
        }

        public async Task SendNewsletterAsync(Newsletter newsletter, List<string> recipients)
        {
            using var client = CreateSmtpClient();
            foreach (var recipient in recipients)
            {
                try
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                        Subject = newsletter.Subject,
                        Body = newsletter.Body,
                        IsBodyHtml = true
                    };
                    mail.To.Add(recipient);
                    await client.SendMailAsync(mail);
                    await Task.Delay(100); // rate limit
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed newsletter to {Recipient}", recipient);
                }
            }
        }

        public async Task SendContactReplyAsync(ContactMessage message, string reply)
        {
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto'>
                    <h2>আপনার বার্তার উত্তর</h2>
                    <p>প্রিয় {message.Name},</p>
                    <div style='background:#f5f5f5;padding:15px;border-radius:4px'>
                        {reply}
                    </div>
                </div>";

            await SendAsync(message.Email, $"Re: {message.Subject}", body);
        }

        private SmtpClient CreateSmtpClient() => new(_settings.SmtpHost, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
            EnableSsl = _settings.EnableSsl
        };
    }
}