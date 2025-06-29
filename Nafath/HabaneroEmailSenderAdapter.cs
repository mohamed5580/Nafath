using Microsoft.AspNetCore.Identity.UI.Services;
using Habanero.Util;
using System.Threading.Tasks;

public class HabaneroEmailSenderAdapter : IEmailSender
{
    private readonly EmailSender _inner;

    public HabaneroEmailSenderAdapter(EmailSender inner)
    {
        _inner = inner;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // استدعاء الدالة الموجودة في Habanero.Util.EmailSender
        _inner.SendAuthenticated(email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
