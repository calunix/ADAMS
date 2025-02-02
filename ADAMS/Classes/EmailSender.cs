using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;

namespace ADAMS.Classes
{
    internal class EmailSender
    {
        private string _smtpServer { get; init; }
        private string _sender { get; init; }
        private string _domain { get; init; }
        private List<string> _copyRecipients { get; init; }

        public EmailSender(IConfiguration config)
        {
            _smtpServer = config.GetValue<string>("Email:SmtpServer");
            _sender = config.GetValue<string>("Email:Sender");
            _domain = config.GetValue<string>("ActiveDirectory:DomainName");
            _copyRecipients = config.GetSection("Email:CopyRecipients").Get<List<string>>();
        }

        public (bool, string) SendNotification(string recipient, string expiry, TimeSpan timeToExpire)
        {
            bool sendSuccess = false;
            string errorMessage = "none";
            SmtpClient smtpClient = new SmtpClient(_smtpServer, 25);
            string subject = $"{_domain} Password Expiration";
            string body = $"Your {_domain} password will expire in {timeToExpire.Days} days, on {expiry}<br><br><a href=\"https://accountservices-rq4vsil.as.northgrum.com/PasswordReset\">Reset Your Password</a>";
            // how to create a multiline string in most readable manner

            MailMessage message = new MailMessage(_sender, recipient, subject, body);
            message.Priority = MailPriority.High;
            message.IsBodyHtml = true;

            foreach (string copyRecipient in _copyRecipients) {
                message.CC.Add(copyRecipient);
            }

            try
            {
                smtpClient.Send(message);
                sendSuccess = true;
            }
            catch (SmtpException ex)
            {
                SmtpStatusCode statusCode = ex.StatusCode;
                errorMessage = $"{ex.Message} Status Code: {statusCode}";
            }
            finally
            {
                smtpClient.Dispose();
            }
            return (sendSuccess, errorMessage);
        }
    }
}
