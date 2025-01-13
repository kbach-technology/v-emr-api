using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using EMR.Application.Interfaces.Services;
using EMR.Shared.Wrapper;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace EMR.Firebase.Services;

public class PushService : IPushService
{
    private readonly FirebaseMessaging _firebaseMessaging;

    public PushService(IConfiguration configuration)
    {
        var firebaseConfig = configuration.GetSection("Firebase");
        var credentialPath = firebaseConfig["CredentialPath"];

        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(credentialPath)
        });

        _firebaseMessaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<Result<string>> PushForecastsAsync(string title, string body,
        Dictionary<string, string> data = null)
    {
        var message = new Message()
        {
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Data = data,
            Topic = "ALL_USERS",
        };

        try
        {
            string response = await _firebaseMessaging.SendAsync(message);
            return await Result<string>.SuccessAsync("Successfully sent message to all users.");
        }
        catch (Exception ex)
        {
            return await Result<string>.FailAsync(ex.Message);
        }
    }

    public async Task<Result<string>> PushUsersAsync(List<string> deviceTokens, string title,
        string body, Dictionary<string, string> data = null)
    {
        var message = new MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Data = data,
            // Ensure the message is received by both iOS and Android
            Android = new AndroidConfig()
            {
                Priority = Priority.High,
            },
            Apns = new ApnsConfig()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "apns-priority", "10" }
                }
            }
        };

        try
        {
            BatchResponse response = await _firebaseMessaging.SendEachForMulticastAsync(message);
            return await Result<string>.SuccessAsync("Successfully sent message to specific users.");
        }
        catch (Exception ex)
        {
            return await Result<string>.FailAsync(ex.Message);
        }
    }
}