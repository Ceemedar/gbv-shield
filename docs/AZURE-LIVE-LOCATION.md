# Azure rollout checklist

The mobile app's currently working Firebase route remains the safe fallback until the Azure subscription issue is resolved. The Azure API in `backend/GBVShield.Api` is ready for a consumption-based container deployment and Azure Table Storage; it does not require a continuously billed VM.

## Before deployment

1. Confirm the nonprofit sponsorship subscription is active and selected.
2. Create budgets at 50%, 75%, 90%, and 100% of the monthly allocation, notifying at least two administrators.
3. Create a dedicated resource group in South Africa North where service availability permits.
4. Store configuration in Container Apps secrets; never paste keys into source control.
5. Enable MFA, activity-log alerts, Defender recommendations, diagnostic logs, and resource locks.
6. Deploy the API with minimum replicas `0`, maximum replicas `2`, and an HTTP concurrency limit.
7. Use a storage account with TLS 1.2+, public network access constrained where feasible, no anonymous blob access, and a lifecycle rule deleting expired location rows.
8. Set `AppConfig.LiveLocationProvider` to `Azure`, replace its placeholder URLs, build a signed internal-test APK, and test on a physical phone before production.

## Safety acceptance test

- Trigger SOS while the app is foregrounded, backgrounded, and screen-locked.
- Walk or drive a known route for 20 minutes; verify helper updates and accuracy timestamps.
- Disable/re-enable mobile data and confirm queued retries recover without creating a new session.
- Stop the session and verify the viewer link immediately stops returning coordinates.
- Wait past expiry and verify the link stays unavailable.
- Confirm running, normal handling, and transport vibration do not falsely trigger SOS.
- Test low battery mode, denied GPS, no SIM airtime, device restart, and Android force-stop.

Android force-stop is an operating-system hard boundary: no app may restart itself until the user opens it again. The persistent notification must remain visible while background shake/location protection is active.
