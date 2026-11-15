using GBV_Shield.Services;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace GBV_Shield
{
    public partial class App : Application
    {
        private readonly AuthService _auth;
        private readonly ReportsService _reports;
        private Page _rootPage;

        // Keep DI service provider accessible globally
        public static IServiceProvider Services { get; private set; } = default!;

        // DI-friendly constructor (matches your MauiProgram registrations)
        public App(AuthService auth, ReportsService reports, IServiceProvider services)
        {
            InitializeComponent();

            _auth = auth;
            _reports = reports;
            Services = services;

            // Global crash guards
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[CRASH GUARD] UnhandledException: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                e.SetObserved();
                System.Diagnostics.Debug.WriteLine($"[CRASH GUARD] UnobservedTaskException: {e.Exception}");
            };

            // Startup page (matches your project)
            _rootPage = new Features.Common.SplashPage(_auth, _reports);

            // Start only when the user has explicitly enabled shake-to-SOS.
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    ShakeDetectionService.EnsureMonitoring();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShakeDetection] Start failed: {ex.Message}");
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_rootPage);
            window.Title = "GBV Shield";
            return window;
        }

        public void SetRootPage(Page page)
        {
            _rootPage = page;
            if (Windows.Count > 0)
                Windows[0].Page = page;
        }

        protected override void OnStart()
        {
            base.OnStart();
            // Ensure shake continues if app resumes (Start is idempotent)
            try { ShakeDetectionService.EnsureMonitoring(); } catch { }
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Android's opt-in foreground safety service owns monitoring in the background.
        }

        protected override void OnResume()
        {
            base.OnResume();
            try { ShakeDetectionService.EnsureMonitoring(); } catch { }
        }
    }
}
