namespace GBV_Shield.Config
{
    public static class AppConfig
    {
        // Set to deployed HTTPS backend /api/ after activation. Never put API keys here.
        public static string AiChatBaseUrl => "";
        // Keep Firebase active until the Azure endpoint passes the physical-device acceptance test.
        public const string LiveLocationProvider = "Firebase"; // Firebase | Azure

        // From the deployed Azure API overview. Must end in /api/.
        public const string ApiBaseUrl = "https://gbvshieldapi.azurewebsites.net/api/";
    }
}
