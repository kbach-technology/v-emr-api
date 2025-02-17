using System.Net;
using System.Net.Mail;
using System.Text;
using EMR.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;

namespace EMR.SendGrid.Services;

public class SendGridService : ISendGridService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger _trace;

    public SendGridService(IConfiguration configuration, ILogger trace)
    {
        _configuration = configuration;
        _trace = trace;
    }

    public async Task SendGridEmailAsync(string toEmail, string subject, string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("SendGrid API key is missing or empty in configuration.");

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_configuration["SendGrid:From"], "COMBO");
            var to = new EmailAddress(toEmail);
            var plainTextContent = $"Your PIN code is: {message}";
            var htmlContent = $"<p>Your PIN code is: <strong>{message}</strong></p>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Failed to send email. Status Code: {response.StatusCode}, Body: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending email: {ex.Message}", ex);
        }
    }

    public async Task SendPinEmailAsync(string to, string toUsername, string subject, string body,
        CancellationToken cancellationToken)
    {
        var smtpSettings = _configuration.GetSection("MailConfiguration");

        var smtpClient = new SmtpClient(smtpSettings["Host"])
        {
            Port = int.Parse(smtpSettings["Port"]),
            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
            EnableSsl = true,
            UseDefaultCredentials = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpSettings["Username"], "GOJOR", Encoding.UTF8),
            Subject = subject,
            Body = TemplatePasswordResetEmail(to, toUsername, body).Body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _trace.Information("Email sent successfully");
        }
        catch (Exception ex)
        {
            _trace.Error(ex, ex.Message);
        }
    }

    public async Task SendUsernameEmailAsync(string to, string toUsername, string subject, string body,
        CancellationToken cancellationToken)
    {
        var smtpSettings = _configuration.GetSection("MailConfiguration");

        var smtpClient = new SmtpClient(smtpSettings["Host"])
        {
            Port = int.Parse(smtpSettings["Port"]),
            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
            EnableSsl = true,
            UseDefaultCredentials = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpSettings["Username"], "GOJOR", Encoding.UTF8),
            Subject = subject,
            Body = TemplateSendUserToEmail(to, toUsername).Body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _trace.Information("Email sent successfully");
        }
        catch (Exception ex)
        {
            _trace.Error(ex, ex.Message);
        }
    }

    private MailMessage TemplatePasswordResetEmail(string toEmail, string fullname, string verificationCode)
    {
        return new MailMessage
        {
            From = new MailAddress(toEmail, "MD EMR"),
            Body = $@"
                    <!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Password Reset Verification</title>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                line-height: 1.6;
                                color: #333;
                                max-width: 600px;
                                margin: 0 auto;
                                padding: 20px;
                            }}
                            .header {{
                                background-color: #007bff;
                                color: white;
                                padding: 20px;
                                text-align: center;
                            }}
                            .content {{
                                padding: 20px;
                                background-color: #ffffff;
                            }}
                            .code-container {{
                                text-align: center;
                                margin: 25px 0;
                                padding: 15px;
                                background-color: #f8f9fa;
                                border-radius: 8px;
                                border: 1px solid #dee2e6;
                            }}
                            .code {{
                                font-size: 32px;
                                font-weight: bold;
                                letter-spacing: 8px;
                                color: #0056b3;
                            }}
                            .warning {{
                                color: #dc3545;
                                font-weight: bold;
                            }}
                            .footer {{
                                margin-top: 30px;
                                text-align: center;
                                font-size: 0.8em;
                                color: #6c757d;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class=""header"">
                            <h1>Password Reset Request</h1>
                        </div>
                        <div class=""content"">
                            <p>Dear {fullname},</p>
                            <p>We received a request to reset your EMR account password. To proceed, please use the following verification code:</p>
                            <div class=""code-container"">
                                <span class=""code"">{verificationCode}</span>
                            </div>
                            <p class=""warning"">This code will expire in 5 minutes for security purposes.</p>
                            <p>If you didn't request this password reset, please:</p>
                            <ul>
                                <li>Ignore this email - your password will remain unchanged</li>
                                <li>Contact our support team immediately if you believe this is suspicious activity</li>
                            </ul>
                            <p>Best regards,<br>MD EMR Security Team</p>
                        </div>
                        <div class=""footer"">
                            <p>This is an automated message. Please do not reply.</p>
                            <p>&copy; 2025 MD EMR. All rights reserved.</p>
                        </div>
                    </body>
                    </html>
                ",
            Subject = "MD EMR Password Reset Verification Code",
            IsBodyHtml = true
        };
    }

    private MailMessage TemplateSendUserToEmail(string toEmail, string username)
    {
        return new MailMessage
        {
            From = new MailAddress(toEmail, "GOJOR"),
            Body = $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Username Reminder</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        color: #333;
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                    }}
                    .header {{
                        background-color: #f4f4f4;
                        padding: 20px;
                        text-align: center;
                    }}
                    .content {{
                        padding: 20px;
                    }}
                    .username-container {{
                        text-align: center;
                        margin: 20px 0;
                        padding: 10px;
                        background-color: #e9e9e9;
                        border-radius: 5px;
                    }}
                    .username {{
                        font-size: 24px;
                        font-weight: bold;
                    }}
                    .footer {{
                        margin-top: 20px;
                        text-align: center;
                        font-size: 0.8em;
                        color: #666;
                    }}
                </style>
            </head>
            <body>
                <div class=""content"">
                    <p>Dear {username},</p>
                    <p>You recently requested a reminder of your username. Here it is:</p>
                    <div class=""username-container"">
                        <span class=""username"">{username}</span>
                    </div>
                    <p>For security reasons, please take the following precautions:</p>
                    <ul>
                        <li>Memorize your username and delete this email immediately after reading.</li>
                        <li>Do not share your username or password with anyone.</li>
                        <li>If you suspect any unauthorized access to your account, please change your password immediately.</li>
                    </ul>
                    <p>If you did not request this username reminder, please contact our support team immediately.</p>
                    <p>If you need further assistance, please don't hesitate to reach out to our support team.</p>
                    <p>Best regards,<br>GOJOR Team</p>
                </div>
                <div class=""footer"">
                    <p>This is an automated message, please do not reply directly to this email.</p>
                    <p>&copy; 2024 GOJOR. All rights reserved.</p>
                </div>
            </body>
            </html>
        ",
            Subject = "GOJOR Username Reminder",
            IsBodyHtml = true
        };
    }
}