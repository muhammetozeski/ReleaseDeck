# Release Deck

Prepare and deliver a multi-episode video release: inspect actual local media, check the release package, review episode metadata together, upload privately to an authorized YouTube channel and reconcile incomplete remote actions before proceeding.

This project is being implemented. It is not counted as a delivered product. Channel operations require a configured OAuth application and each channel owner's explicit authorization. No upload, publication, audience or revenue is claimed from local fixtures.

The permanent source is this directory. The intended primary application path is `/release-deck/` on the existing BinSite origin; no additional public hosting project is needed. The browser application uses C#/.NET10 Blazor. Existing main-site creator accounts and subscription entitlements will be reused instead of introducing another payment account. The release API lives under `/api/commerce/release-deck/` so the existing account cookie reaches it.

Start with [the product contract](docs/Product-Contract.md). API and implementation knowledge belongs in `docs/`; dated continuation records belong in `Walkthrough/`. Follow the existing [BinSite documentation guide](../BinSite/docs/Documentation-Guide.md) and [handover guide](../BinSite/Walkthrough/Walkthrough-Guide.md). Product UI and project documentation are English; user conversation is Turkish.
