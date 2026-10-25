# Release Deck client snapshot

The client is `src/ReleaseDeck.Portal`; portable document rules and exports are in `src/ReleaseDeck.Core`. This is an implemented source/build snapshot, not a claim of completed publication or authorized Google delivery. Hosting, account configuration and final acceptance belong to the integrating main site.

## Working surfaces

- **Release library:** separate local drafts, strict project-file import, clearly identified original planning examples, actual account status, online releases and invitation joining. Local work is available while the online list is loading or unavailable.
- **Workspace:** actual selected video preview, native inspection, episode ordering and metadata, translations, audience decisions, local-time planning, complete-file identification, exact reattachment and a captured JPEG frame. The three packaged sample clips enter the same media pipeline; no sample creates a channel operation.
- **Team review:** saved version text, actual review records, owner approval, editor/viewer invitations, pending-invitation revocation, member-role changes and archive/restore. A viewer can make a separate local copy. Only the owner manages access or approves delivery.
- **Delivery:** actual configuration/connection reads, owner authorization, private-upload creation, transfer progress, pause/resume, authoritative reconciliation, cancellation, approved metadata and separate visibility actions. An explicit restart warns about a possible earlier private video. Unconfigured channel actions remain unavailable.

See [the API contract](Api-Contract.md) for server authority and [the media contract](Media-Transfer.md) for the direct-browser transfer and tree identity. Tokens, upload URLs, object URLs, browser file handles and captured frames do not enter the portable/shared document. Invited collaborators do not receive channel data through these UI reads.

## Document and action boundaries

Schema 1 contains only release/episode metadata and optional file identities. Import rejects unknown or repeated fields, malformed values and unsupported versions. The complete-file algorithm is `rd-sha256-tree-4m-v1`, not flat SHA-256. Preview precedes identification; an imported file identity alone never counts as an attached original. UTC release timestamps are serialized explicitly; local input is converted before saving.

Project export is editable JSON. The release ZIP contains that JSON, an escaped standalone HTML release sheet and instructions; it does not contain media. Captured frames are separate JPEG downloads. Local library writes are serialized, preserve the active in-memory draft on storage failure, and remain separate from the online document. Reloading the same online body reuses its matching local entry; opening a changed remote version preserves an earlier divergent local draft.

If saved browser data cannot be read, initialization completes in an explicitly warned, memory-only session. The previous stored value is not overwritten. New releases and imported projects can still be edited and downloaded; the warning stays visible across pages. Unsafe null records/collections, unsupported envelope fields and invalid file-identity metadata take this recovery path. Editable incomplete language fields remain valid local drafts. A rejected storage write also keeps the active work available for download and stops further writes during that session.

One outer UI boundary reports each failed action without declaring dependent success. Diagnostics contain action timing/error types and bounded request status, not creator text or credentials. Writes are not automatically retried. Save request identities and frozen bodies survive a repeat within the mounted page instance; **they are not yet persisted across a full reload**. After an unconfirmed create and reload, inspect the online library before creating another copy. Metadata/publication intents retain their request identity within the delivery page; changed pending intent requires reconciliation.

Remote operation rows are authoritative; client progress is temporary feedback. Publication controls require the approved revision and returned applied metadata revision. Visibility consent is separate per operation. Unsaved local content is distinguished from the saved version used for delivery. Connection removal distinguishes a confirmed provider revocation from local removal only. Page disposal pauses local transfer and clears transient access; it does not claim remote deletion or recall of an already issued Google grant.

Preview changes are serialized against the current episode, local-document generation and file binding. Selecting an unattached episode clears the previous video; a late import or mount reapplies the current selection. Unaccepted reattachments release their new browser handles. Transfer feedback includes the bridge's actionable message rather than only its phase code. Feedback and invitation inputs update their action state while typing. Empty releases retain project-file saving, while release-package download waits for an episode.

## Build and host contract

From this repository root:

```powershell
./scripts/Publish-App.ps1 -NoRestore -Destination '<BinSite-public-directory>/release-deck' -BasePath '/release-deck/'
```

The cached .NET 10 dependencies suffice for the local build. Omit `-NoRestore` in a clean environment. The publisher copies the actual static application, packaged samples and bridge modules, refreshes compressed bootstraps, and emits scoped headers/explicit SPA routes. The parent host merges those metadata files at its root. There is no rewrite wildcard that intercepts static assets. API calls intentionally use `/api/commerce/release-deck/`; account status uses `/api/commerce/status` and sign-in links use `/uyelik/?returnTo=%2Frelease-deck%2F`.

Owned appearance markers are `data-rd-theme`, `data-rd-appearance=style`, `data-rd-appearance=script` and `ReleaseDeckAppearance.audit`. The product uses its own layout and the previously audited contrast algorithm adapted to its marker names. The observer covers disabled controls. Media pixels are outside text-color repair. The default is dark; theme choice is stored locally.

## Evidence and unfinished boundaries

The Release build completed with zero warnings/errors. A real mounted Release publish completed to a disposable verification directory. Twenty-six earlier C# assertions exercised the production domain/import/export code: identity matching, schema/unknown-field rejection, independent clones, incomplete decisions, UTF-8 descriptions, restored-file detachment, HTML encoding, UTC instant preservation and a usable three-file ZIP. Those unchanged assertions were not repeated for the final UI pass. The disposable consumer is `C:\C\ReleaseDeckDomain`.

Fourteen targeted checks against the compiled production Store/Workspace types covered corrupt/null/unknown saved data, invalid file metadata, retained raw storage, recovery import and actual JSON/three-entry ZIP creation, denied reads/writes, editable incomplete language fields, detached-preview clearing, delayed import/mount selection changes and stale reattachment cleanup. The consumer is `C:\C\ReleaseDeckDomain\ClientRecovery`. Four separate checks exercised the actual `app.js` read/write exports with controlled browser storage. These checks use controlled JavaScript interop/storage and do not claim a native codec or graphical-browser journey. Bridge evidence is recorded separately in the media contract; its controlled responses are not a live Google upload.

The source snapshot still needs the main host's schema/API integration and publication acceptance. No owner Google consent, native end-to-end graphical interaction, authorized upload/publication, customer use or revenue is claimed here. The cross-reload pending-save boundary above remains explicit when this snapshot is resumed.
