using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ISLEMLER.Controllers.User;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
namespace ISLEMLER.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public UserController(ILogger<UserController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        
        [HttpPost]
        [Route("SendMail")]
        public void SendEmail(string recipientEmail, string subject, string body)
{
    // Create a new SmtpClient instance
    SmtpClient client = new SmtpClient("smtp.gmail.com", 587); // Use appropriate SMTP server and port

    // Set the credentials
    client.Credentials = new NetworkCredential("muhendis.birgulayaz@gmail.com", "yisnagjgjfcszzxd");
    client.EnableSsl = true; // Enable SSL if required

    // Create a MailMessage instance
    MailMessage mailMessage = new MailMessage();
    mailMessage.From = new MailAddress("engineer.birgul@gmail.com");
    mailMessage.To.Add(recipientEmail);
    mailMessage.Subject = subject;
    mailMessage.Body = body;
    mailMessage.IsBodyHtml = true; // Set to true if body contains HTML

    try
    {
        // Send the email
        client.Send(mailMessage);
    }
    catch (Exception ex)
    {
        // Handle any errors that may have occurred
        Console.WriteLine($"Error sending email: {ex.Message}");
    }
}
    }

    public class EmailRequest
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
