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

    public SmsService(IConfiguration configuration, ILogger trace, IStringLocalizer<SmsService> _localizer)
    {
        _configuration = configuration;
        _trace = trace;
        this._localizer = _localizer;
    }

    public void SendOtpAsync(string to, string code)
    {
        try
        {
            var username = _configuration.GetSection("MekongNet:Username").Value;
            var passwrod = _configuration.GetSection("MekongNet:Password").Value;
            var sender = _configuration.GetSection("MekongNet:Sender").Value;
            var body = $"{code} is your verification code for GOJOR. Do not share this Code with anyone.";
            var urlSms = "https://api.mekongsms.com/api/postsms.aspx";
            var postParam = "username=" + username + "&pass=" + passwrod + "&cd=" +
                            "&sender=" + sender + "&smstext=" +
                            HttpUtility.UrlEncode(body) + "&gsm=" + to;
            var uri = new Uri(urlSms);
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "application/x-www-form-urlencoded";
            var writer = new StreamWriter(request.GetRequestStream());
            writer.Write(postParam);
            writer.Close();
            var response = (HttpWebResponse)request.GetResponse();
            var reader = new StreamReader(response.GetResponseStream());
            response.Close();
        }
        catch (Exception ex)
        {
            _trace.Debug("Send OTP failed: ", ex.Message);
        }
    }
}