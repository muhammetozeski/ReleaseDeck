# Host setup and channel connection

This is the service owner's setup guide. It does not belong in the creator's editing workflow. Ordinary creators prepare local releases without credentials, and use their existing BinSite account for shared releases. Private creator work does not load advertising.

## Existing deployment

BinSite publishes the pinned Release Deck repository into `/release-deck/`. The main workflow applies the additive schema before deploying functions that count release projects, preserves the dedicated encryption key and publishes the hourly D1 cleanup Worker. It checks the actual configured schedule before setting the maintenance-ready flag. Existing account and subscription providers remain in charge of the shared free/paid allowance.

## Enable YouTube connection

Create a dedicated **web application** OAuth client in the Google project with YouTube Data API v3 enabled. Register this exact authorized redirect:

```text
https://muhammetozeski-binsite.pages.dev/api/commerce/release-deck/oauth/callback
```

Configure the Google consent screen and its allowed/test users as appropriate, then download that web client's JSON file. The [web-server authorization guide](https://developers.google.com/identity/protocols/oauth2/web-server) describes the client/redirect setup. The requested scope and upload/publication restrictions are described in [the integration contract](Google-Integration.md); credentials alone do not establish Google's verification, audit or publication eligibility.

The application's public home is `https://muhammetozeski-binsite.pages.dev/release-deck/` and its privacy page is `https://muhammetozeski-binsite.pages.dev/privacy/`. Use these published addresses in the consent screen rather than a local development URL.

From this repository directory, run:

```powershell
./scripts/Configure-Google.ps1 -ClientFile 'C:\Private\youtube-web-client.json'
```

The existing GitHub CLI sign-in must have permission to manage the `muhammetozeski/BinSite` repository's Actions secrets and dispatch its deployment workflow. No separate Cloudflare token needs to be entered into this tool; the existing deployment already holds it.

The tool saves the pair together in one encrypted repository secret, `RELEASE_GOOGLE_WEB_CLIENT`, via standard input and starts the existing production workflow. It neither prints the credential nor adds it to Git. Publication copies only the two supplied Google fields to the corresponding Cloudflare production bindings; unrelated secrets are not replayed or replaced. Run it once after client creation, and again when replacing that client. The website and hourly Worker do not run this owner tool.

After publication succeeds, a creator opens **Delivery → Connect YouTube**, explicitly authorizes their own channel, and reviews the returned channel name. Selecting sample/local videos never starts consent or an upload. Private upload and later visibility/scheduling are separate owner actions.

## Commercial setup

Release Deck reuses the main site's creator subscription and actual combined saved-project allowance. Existing [membership provider setup](https://github.com/muhammetozeski/BinSite/blob/main/docs/Revenue-Architecture.md) handles checkout, provider notifications, entitlements, cancellation and billing portals. Do not create a second subscription account or show an active checkout without its configured provider. Demand, paying customers, channel delivery and revenue are separate observations.

## Troubleshooting

- A desktop OAuth client is refused by the configuration tool; use the registered web client.
- An unavailable connection does not block local preparation or project/package downloads.
- A deployment failure leaves Google actions unavailable until the actual schema, encryption and scheduled maintenance prerequisites are present.
- After an uncertain upload, check the recorded remote status before explicitly resuming. Do not create a second upload as a recovery shortcut.

No existing personal YouTube credential is read by the setup tool. The creator authorizes the configured application through Google.

Implementation evidence: PowerShell parsing and a local `-WhatIf` invocation with a disposable fixture file passed. No credential was stored and no workflow was dispatched by that dry run. The actual encrypted secret operation uses the installed GitHub CLI's documented standard-input contract; owner-supplied credential configuration has not been performed during implementation.
