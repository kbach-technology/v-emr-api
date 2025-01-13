namespace EMR.Shared.Constants.Application;

public static class ApplicationConstants
{
    public static class SignalR
    {
        public const string HubUrl = "/signalRHub";
        public const string HubRootUrlPro = "https://prod.api.fyg-medical.com" + HubUrl;
        public const string HubRootUrlDev = "https://prod.api.fyg-medical.com" + HubUrl;
        public const string SendUpdateDashboard = "UpdateDashboardAsync";
        public const string ReceiveUpdateDashboard = "UpdateDashboard";
        public const string SendRegenerateTokens = "RegenerateTokensAsync";
        public const string ReceiveRegenerateTokens = "RegenerateTokens";
        public const string ReceiveChatNotification = "ReceiveChatNotification";
        public const string SendChatNotification = "ChatNotificationAsync";
        public const string ReceiveMessage = "ReceiveMessage";
        public const string SendMessage = "SendMessageAsync";

        public const string OnConnect = "OnConnectAsync";
        public const string ConnectUser = "ConnectUser";
        public const string OnDisconnect = "OnDisconnectAsync";
        public const string DisconnectUser = "DisconnectUser";
        public const string OnChangeRolePermissions = "OnChangeRolePermissions";
        public const string LogoutUsersByRole = "LogoutUsersByRole";

        public const string PingRequest = "PingRequestAsync";
        public const string PingResponse = "PingResponseAsync";
    }

    public static class Cache
    {
        public const string GetAllDevicesCacheKey = "all-devices";
        public const string GetAllAppVersionsCacheKey = "all-app-versions";

        public const string GetAllCountriesCacheKey = "all-countries";

        public static string GetAllEntityExtendedAttributesCacheKey(string entityFullName)
        {
            return $"all-{entityFullName}-extended-attributes";
        }

        public static string GetAllEntityExtendedAttributesByEntityIdCacheKey<TEntityId>(string entityFullName,
            TEntityId entityId)
        {
            return $"all-{entityFullName}-extended-attributes-{entityId}";
        }
    }

    public static class MimeTypes
    {
        public const string OpenXml = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    public static class Message
    {
        public const string Got = "The record has been retrieved successfully.";
        public const string Created = "The record has been created successfully.";
        public const string Saved = "The record has been saved successfully.";
        public const string Failed = "There was an error saving the record. Please try again.";
        public const string Updated = "The record has been updated successfully.";
        public const string Deleted = "The record has been deleted successfully.";
        public const string NotFound = "The record you requested could not be found.";
        public const string Exists = "A record with the same information already exists.";
        public const string Approved = "The record has already been approved.";
        public const string RejectReason = "Please provide a reason for rejecting the record.";
        public const string Icon = "Both large and small icons are required.";
        public const string RequiredInfo = "Please complete all required fields.";
        public const string VerifyCodeSend = "The verification code has been sent successfully.";
        public const string Push = "The notification has been pushed successfully.";
        public const string UnPush = "There was an error pushing the notification. Please try again later.";
        public const string ExistsRequestReschedule = "You have already requested a reschedule.";

        public const string InvalidedCredencial =
            "The provided credentials are invalid. Please check your username and password and try again.";

        public const string LoginSuccess = "You have successfully logged in!";
        public const string InActiveUser = "User is Inactive. Please contact the administrator.";
        public const string InValidRefreshToken = "Invalid client refresh token.";
        public const string ExistsUserName = "The username is already in used, Please provide different username.";
        public const string AvailableUserName = "Available";
    }
}