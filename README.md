# GBV Shield

A mobile safety application built with C# and .NET MAUI, with an ASP.NET Core backend and Firebase services.

## Features represented in this source

- Account sign-in and registration
- SOS messages to user-configured emergency contacts
- Permission-based live location sharing
- Support directory and nearby service lookup
- Incident reports and local storage
- Community stories and chat screens
- A separate backend for live tracking and AI chat integration

## Technology

.NET MAUI, C#, XAML, ASP.NET Core, Firebase Authentication and Realtime Database, SQLite, Android location and SMS APIs, and Azure infrastructure templates.

## Source and validation status

This portfolio copy was prepared from the owner's September 2026 project backup. The owner reports that the apps have been fully tested. This copy has not been independently built or device-tested during portfolio preparation.

The included AI chat documentation describes the integration as implemented but not activated in this source snapshot. It requires a backend URL and server-side provider configuration. That source limitation should not be confused with the owner's separately tested deployment.

## Setup

1. Install the .NET SDK selected by `global.json`, the MAUI workloads, and the Android development tools.
2. Open `GBV_Shield.sln` in a compatible Visual Studio installation.
3. Configure your own public Firebase client settings in `Constants.cs` and provide the platform Firebase configuration files locally.
4. Review `Config/AppConfig.cs` and set backend URLs for your own environment.
5. Supply backend secrets using environment variables or a secrets manager. See `backend/GBVShield.Api/README.md` and `backend/GBVShield.Api/AI-CHAT.md`.
6. Supply release signing credentials through protected build properties. No signing keys are included.

## Project layout

| Folder | Purpose |
| --- | --- |
| `Features/` | App screens and feature view models |
| `Services/` | Authentication, location, SOS, reports and data services |
| `Models/` | Shared application models |
| `Platforms/` | Android and iOS integration |
| `backend/` | ASP.NET Core API and automated checks |
| `infra/` | Azure infrastructure templates |
| `Resources/` | App icons and visual assets |

This repository excludes credentials, signing keys, local Firebase configuration, build outputs and personal documents. The mobile app requires the user's device permissions and configuration before its safety features can operate.
