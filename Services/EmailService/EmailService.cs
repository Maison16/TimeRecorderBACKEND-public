using MailKit.Net.Smtp;
using MimeKit;

namespace TimeRecorderBACKEND.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _from;
        private readonly string _password;

        public EmailService(string from, string password)
        {
            _from = from;
            _password = password;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_from, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
