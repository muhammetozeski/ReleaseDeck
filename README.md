# Release Deck

Prepare and deliver a multi-episode video release: inspect actual local media, check the release package, review episode metadata together, upload privately to an authorized YouTube channel and reconcile incomplete remote actions before proceeding.

Current live delivery is recorded in [BinSite's publication ledger](https://github.com/muhammetozeski/BinSite/blob/main/publication.json). Channel operations require a configured OAuth application and each channel owner's explicit authorization. Uploads, publication, audience and revenue are recorded only when observed.

The permanent source is this directory. The intended primary application path is `/release-deck/` on the existing BinSite origin; no additional public hosting project is needed. The browser application uses C#/.NET10 Blazor. Existing main-site creator accounts and subscription entitlements will be reused instead of introducing another payment account. The release API lives under `/api/commerce/release-deck/` so the existing account cookie reaches it.

Start with [the knowledge index](docs/İçindekiler.md) and [product contract](docs/Product-Contract.md). API and implementation knowledge belongs in `docs/`; dated continuation records belong in `Walkthrough/`. Follow the [BinSite documentation guide](https://github.com/muhammetozeski/BinSite/blob/main/docs/Documentation-Guide.md) and [handover guide](https://github.com/muhammetozeski/BinSite/blob/main/Walkthrough/Walkthrough-Guide.md). Product UI and project documentation are English; user conversation is Turkish.

## Source and publication

Public source: https://github.com/muhammetozeski/ReleaseDeck . The main site's existing workflow fetches the exact commit in its mounted-source manifest and builds this app into its own path. `scripts/Publish-App.ps1` also produces a portable static web bundle with scoped hosting metadata. No private video or credential is part of the source or bundle.

## Channel owner setup

After creating the dedicated Google web client and registering its production callback, the service owner runs `scripts/Configure-Google.ps1` with that downloaded client file. It stores one encrypted GitHub Actions secret and dispatches the existing deployment. The [owner setup guide](docs/Owner-Setup.md) describes that single configuration step, the per-creator channel authorization and the shared membership system. Local inspection and project/package output do not require Google setup.
