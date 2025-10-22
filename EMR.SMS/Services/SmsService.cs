using System.Net;
using System.Web;
using EMR.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Serilog;

namespace EMR.SMS.Services;

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SmsService> _localizer;
    private readonly ILogger _trace;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10) // Prevent hanging on slow SMS API
    };

    public SmsService(IConfiguration configuration, ILogger trace, IStringLocalizer<SmsService> _localizer)
    {
        _configuration = configuration;
        _trace = trace;
        this._localizer = _localizer;
    }

    public void SendOtpAsync(string to, string code)
    {
        // Fire-and-forget: Run asynchronously without blocking caller
        _ = Task.Run(async () =>
        {
            try
            {
                var username = _configuration.GetSection("MekongNet:Username").Value;
                var password = _configuration.GetSection("MekongNet:Password").Value;
                var sender = _configuration.GetSection("MekongNet:Sender").Value;
                var body = $"{code} is your verification code for GOJOR. Do not share this Code with anyone.";
                var urlSms = "https://api.mekongsms.com/api/postsms.aspx";

                var postParam = $"username={username}&pass={password}&cd=&sender={sender}&smstext={HttpUtility.UrlEncode(body)}&gsm={to}";
                var content = new StringContent(postParam, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await _httpClient.PostAsync(urlSms, content);
                response.EnsureSuccessStatusCode();

                _trace.Information($"SMS sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _trace.Error(ex, $"Send OTP failed to {to}: {ex.Message}");
            }
        });
    }
}