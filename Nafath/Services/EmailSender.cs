using Microsoft.AspNetCore.Identity.UI.Services;

namespace Nafath.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // your SMTP or third‑party logic here
            return Task.CompletedTask;
        }
    }
}
