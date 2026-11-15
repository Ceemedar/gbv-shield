namespace GBV_Shield
{
    public static class Constants
    {
        // Configure these public Firebase client values for your own project.
        public const string FirebaseApiKey = "YOUR_FIREBASE_WEB_API_KEY";
        public const string FirebaseProjectId = "gbv-shield";
        public const string FirebaseDatabaseUrl = "https://gbv-shield-default-rtdb.firebaseio.com";

        // 🔥 Default search radius for shelters & police (meters)
        public const int ShelterSearchRadius = 5000; // 5 km
    }
}
