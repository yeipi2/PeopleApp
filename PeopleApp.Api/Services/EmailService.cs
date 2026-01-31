using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PeopleApp.Api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code, string userName)
    {
        try
        {
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"];
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Código de Verificación - PeopleApp";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #ff6b35 0%, #ff8c42 100%); padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>PeopleApp</h1>
                        </div>
                        <div style='padding: 30px; background-color: #f9f9f9;'>
                            <h2 style='color: #333;'>Hola {userName},</h2>
                            <p style='color: #666; font-size: 16px;'>Tu código de verificación es:</p>
                            <div style='background-color: white; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                                <h1 style='color: #ff6b35; font-size: 36px; letter-spacing: 8px; margin: 0;'>{code}</h1>
                            </div>
                            <p style='color: #666; font-size: 14px;'>Este código expira en <strong>10 minutos</strong>.</p>
                            <p style='color: #999; font-size: 12px; margin-top: 30px;'>Si no solicitaste este código, ignora este correo.</p>
                        </div>
                        <div style='background-color: #333; padding: 15px; text-align: center;'>
                            <p style='color: #999; font-size: 12px; margin: 0;'>© 2026 PeopleApp. Todos los derechos reservados.</p>
                        </div>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email de verificación enviado a {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de verificación a {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> Send2FACodeAsync(string toEmail, string code, string userName)
    {
        try
        {
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"];
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Código de Inicio de Sesión - PeopleApp";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #ff6b35 0%, #ff8c42 100%); padding: 20px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>🔒 Inicio de Sesión Seguro</h1>
                        </div>
                        <div style='padding: 30px; background-color: #f9f9f9;'>
                            <h2 style='color: #333;'>Hola {userName},</h2>
                            <p style='color: #666; font-size: 16px;'>Alguien está intentando iniciar sesión en tu cuenta.</p>
                            <p style='color: #666; font-size: 16px;'>Tu código de verificación es:</p>
                            <div style='background-color: white; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                                <h1 style='color: #ff6b35; font-size: 36px; letter-spacing: 8px; margin: 0;'>{code}</h1>
                            </div>
                            <p style='color: #666; font-size: 14px;'>Este código expira en <strong>10 minutos</strong>.</p>
                            <p style='color: #d32f2f; font-size: 14px; margin-top: 20px;'><strong>⚠️ Si no fuiste tú, cambia tu contraseña inmediatamente.</strong></p>
                        </div>
                        <div style='background-color: #333; padding: 15px; text-align: center;'>
                            <p style='color: #999; font-size: 12px; margin: 0;'>© 2026 PeopleApp. Todos los derechos reservados.</p>
                        </div>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Código 2FA enviado a {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando código 2FA a {Email}", toEmail);
            return false;
        }
    }
    public async Task<bool> SendPasswordChangeCodeAsync(string toEmail, string code, string userName)
    {
        try
        {
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"];
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Cambio de Contraseña - PeopleApp";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #ff6b35 0%, #ff8c42 100%); padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔐 Cambio de Contraseña</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2 style='color: #333;'>Hola {userName},</h2>
                        <p style='color: #666; font-size: 16px;'>Has solicitado cambiar tu contraseña.</p>
                        <p style='color: #666; font-size: 16px;'>Tu código de verificación es:</p>
                        <div style='background-color: white; border: 2px solid #ff6b35; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #ff6b35; font-size: 36px; letter-spacing: 8px; margin: 0;'>{code}</h1>
                        </div>
                        <p style='color: #666; font-size: 14px;'>Este código expira en <strong>10 minutos</strong>.</p>
                        <p style='color: #d32f2f; font-size: 14px; margin-top: 20px;'><strong>⚠️ Si no solicitaste este cambio, ignora este correo y considera cambiar tu contraseña.</strong></p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center;'>
                        <p style='color: #999; font-size: 12px; margin: 0;'>© 2026 PeopleApp. Todos los derechos reservados.</p>
                    </div>
                </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email de cambio de contraseña enviado a {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de cambio de contraseña a {Email}", toEmail);
            return false;
        }
    }
}