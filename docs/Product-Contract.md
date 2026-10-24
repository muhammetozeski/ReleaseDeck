# Product contract

## Completed user job

A creator selects their own episode files, sees actual media previews and preflight issues, prepares the episode metadata/language/order/release times, obtains team feedback and explicitly executes the authorized channel operations. An interrupted action retains its actual remote identity and reconciles before continuing. Each completed upload is initially private. Publication or scheduling requires a separate deliberate owner action. A batch is not an atomic transaction.

The first keyless session must perform real media inspection and produce a usable release package/project file, not merely store a checklist. Project files contain metadata and file identities, not media or OAuth credentials. Original videos travel directly from the user's computer to Google when channel upload is configured and authorized.

## Shared account and data boundary

Use the existing BinSite creator account API at `/api/commerce/` for account creation/recovery/status and existing membership upgrades. The new API is `/api/commerce/release-deck/`. Host adapters supply verified identity/auth helpers; do not accept member IDs, channel tokens or premium claims from the browser.

Release workspaces use their own prefixed tables because the existing workspace schema only permits three earlier document kinds. Their owner/editor/viewer access and optimistic revision behavior must be implemented explicitly. Their active owned-project count participates in the existing three/free and100/subscribed combined allowance. Root will extend the shared quota statements after adding the new tables; do not rebuild existing workspace tables or copy their contents.

Only the channel-owning workspace owner can initiate upload/publication. Editors can prepare metadata; viewers can read the shared release. Remote identities and operation receipts live in server-owned rows, outside the editable project body. Revoking membership or archiving the workspace removes that access. Uploaded-file identity, reviewed revision and explicit approval must be bound to the operation. A stale editor save or delayed response cannot replace a newer draft or operation result.

## Google connection and transfer

Use verified official YouTube/Google OAuth and resumable-upload contracts. Root will provision a dedicated encryption key; Google client credentials remain a separate owner configuration step. Refresh tokens and upload capabilities are private and never stored in portable/shared project bodies or logs. OAuth callbacks require one-use state, PKCE and a browser-bound callback cookie suitable for a cross-site top-level return; the main account cookie alone is SameSite=Strict.

Verify current scope coverage, upload/recovery requests, public/private scheduling restrictions, metadata update replacement behavior, provider data-retention requirements and direct-browser transfer/CORS before relying on them. Existing Studio batch upload is a baseline, not a claimed product advantage. No access to existing personal YouTube credentials or live channel operations is part of implementation testing.

## Product surfaces

Support a release library, an editable release workspace, a focused episode/media inspector, team access/review and a remote-operation view. The frontend owner chooses the complete layout and visual language. Keep limits beside the action they constrain, optional detail behind task-specific disclosures and useful next actions in empty states. Show plain, actionable errors and preserve drafts.

## Monetization and evidence

Use the existing configured creator subscription for larger real storage/collaboration capacity; do not list free features as paid. Do not place advertising trackers in private creator work. Paid remote orchestration is a hypothesis until tested with real users. Local media outputs, provider fixtures, live page delivery, authorized live uploads and commercial results are separate evidence classes. Nothing is counted as a new delivered product until its actual implemented surface is published and verified.
