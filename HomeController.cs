using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ISLEMLER.Services;
using ISLEMLER.Events;
using ISLEMLER.Models;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ISLEMLER.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public HomeController(IConfiguration configuration, UserService userService, ILogger<HomeController> logger)
        {
            _configuration = configuration;
            _userService = userService;
            _logger = logger;
            
            // Subscribe to UserCreated event
            _userService.UserCreated += HandleUserCreated;
        }

        private void HandleUserCreated(object? sender, UserEventArgs e)
        {
            _logger.LogInformation("New user created: {@UserEvent}", e);
        }

        [HttpPost]
        [Route("SendDataToDB")]
        public IActionResult SendDataToDB([FromBody] UserRequest request)
        {
            try
            {
                // Token'dan kullanıcı adını al
                var tokenUsername = User.Identity?.Name;
                _logger.LogInformation("Token username: {TokenUsername}", tokenUsername);

                if (string.IsNullOrEmpty(tokenUsername))
                {
                    _logger.LogError("No username found in token");
                    return Unauthorized("Invalid token");
                }

                // Token'daki kullanıcı adı ile gönderilen kullanıcı adı eşleşmeli
                if (tokenUsername.ToLower() != request.Username.ToLower())
                {
                    _logger.LogWarning("Username mismatch. Token: {TokenUsername}, Request: {RequestUsername}", 
                        tokenUsername, request.Username);
                    return BadRequest("Username mismatch with token");
                }

                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                _logger.LogInformation("SendDataToDB started with user details: {Username}", request.Username);

                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Empty username or password submitted");
                    return BadRequest("Username and password are required");
                }

                // Kullanıcı bilgilerini kontrol et
             /*   if (request.Username != "birgul" || request.Password != "qwerty")
                {
                    _logger.LogWarning("Invalid credentials for user: {Username}", request.Username);
                    return BadRequest("Invalid username or password");
                }
*/
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Database connection string not found");
                    return StatusCode(500, "Database connection string is missing");
                }

                // Create user event args first
                var userEvent = new UserEventArgs
                {
                    Username = request.Username,
                    Email = string.Empty,
                    Age = 33,
                    Timestamp = DateTime.Now
                };

                // Validate user event args
                if (userEvent == null)
                {
                    _logger.LogError("UserEventArgs is null");
                    return BadRequest("Invalid user event data");
                }

                // Log the user event details
                _logger.LogInformation("Created user event: {@UserEvent}", userEvent);

                _logger.LogInformation("Opening database connection...");

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    try 
                    {
                        connection.Open();
                        _logger.LogInformation("Database connection successful");

                        using (var cmd = new NpgsqlCommand())
                        {
                            cmd.Connection = connection;
                            cmd.CommandText = "INSERT INTO \"SecondUsers\" (\"name\", \"email\", \"age\") VALUES (@name, @email, @age)";
                            cmd.Parameters.AddWithValue("name", userEvent.Username);
                            cmd.Parameters.AddWithValue("email", userEvent.Email);
                            cmd.Parameters.AddWithValue("age", userEvent.Age);
                            _logger.LogInformation("Executing SQL command: {SQL}", cmd.CommandText);
                            cmd.ExecuteNonQuery();

                            // Create PDF document
                            using (var document = new Document())
                            {
                                string fileName = $"user_data_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                                string filePath = Path.Combine(Path.GetTempPath(), fileName);
                                
                                PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                                document.Open();

                                // Add content to PDF
                                document.Add(new Paragraph($"User Information"));
                                document.Add(new Paragraph("-------------------"));
                                document.Add(new Paragraph($"Name: {userEvent.Username}"));
                                document.Add(new Paragraph($"Email: {userEvent.Email}"));
                                document.Add(new Paragraph($"Age: {userEvent.Age}"));
                                document.Add(new Paragraph($"Created: {userEvent.Timestamp}"));
                                // Set document properties and styling
                                document.AddTitle("User Information Report");
                                document.AddAuthor("System");
                                document.AddCreationDate();

                                // Add custom fonts and colors
                                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
                                Font titleFont = new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD, BaseColor.DARK_GRAY);
                                Font headingFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.DARK_GRAY);
                                Font normalFont = new Font(Font.FontFamily.HELVETICA, 11, Font.NORMAL, BaseColor.BLACK);

                                // Add logo/header
                                document.Add(new Paragraph("User Information Report", titleFont));
                                document.Add(new Paragraph("\n"));

                                // Create styled table
                                PdfPTable table = new PdfPTable(2);
                                table.WidthPercentage = 90;
                                table.SpacingBefore = 10f;
                                table.SpacingAfter = 10f;

                                // Add table headers
                                table.AddCell(new PdfPCell(new Phrase("Field", headingFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase("Value", headingFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5 });

                                // Add table rows
                                table.AddCell(new PdfPCell(new Phrase("Name:", normalFont)) { Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase(userEvent.Username, normalFont)) { Padding = 5 });
                                
                                table.AddCell(new PdfPCell(new Phrase("Email:", normalFont)) { Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase(userEvent.Email, normalFont)) { Padding = 5 });
                                
                                table.AddCell(new PdfPCell(new Phrase("Age:", normalFont)) { Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase(userEvent.Age.ToString(), normalFont)) { Padding = 5 });
                                
                                table.AddCell(new PdfPCell(new Phrase("Created:", normalFont)) { Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase(userEvent.Timestamp.ToString(), normalFont)) { Padding = 5 });

                                document.Add(table);

                                // Add footer
                                document.Add(new Paragraph("\n"));
                                document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", normalFont));


                                document.Close();

                                // Return file for download
                                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                                System.IO.File.Delete(filePath); // Clean up temp file

                                Response.Headers.Append("Content-Disposition", $"attachment; filename={fileName}");
                                Response.Headers.Append("Content-Type", "application/pdf");
                                Response.Body.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                        }
                    }
                    catch (NpgsqlException dbEx)
                    {
                        _logger.LogError(dbEx, "Database error: {ErrorMessage}", dbEx.Message);
                        return StatusCode(500, $"Database error: {dbEx.Message}");
                    }
                }

                _logger.LogInformation("Database operation successful. Triggering event...");
                
                // Trigger the user created event after successful DB operation
                _userService.CreateUser(userEvent);

                _logger.LogInformation("User {@UserEvent} successfully saved to database", userEvent);
                return Ok("User data saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error: {ErrorType} - {ErrorMessage}", ex.GetType().Name, ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("DownloadPdf")]
        public IActionResult DownloadPdf([FromQuery] string username)
        {
            try
            {
                var tokenUsername = User.Identity?.Name;
                if (string.IsNullOrEmpty(tokenUsername) || tokenUsername.ToLower() != username.ToLower())
                {
                    return Unauthorized("Invalid token or username mismatch");
                }

                using (var document = new Document())
                {
                    string fileName = $"user_data_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    string filePath = Path.Combine(Path.GetTempPath(), fileName);
                    
                    PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                    document.Open();

                    document.Add(new Paragraph($"User Information"));
                    document.Add(new Paragraph("-------------------"));
                    document.Add(new Paragraph($"Name: {username}"));
                    document.Add(new Paragraph($"Date: {DateTime.Now}"));

                    document.Close();

                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    System.IO.File.Delete(filePath);

                    return File(fileBytes, "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed");
                return StatusCode(500, "Error generating PDF");
            }
        }
    }
}
