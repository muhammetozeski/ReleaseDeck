# Release Deck API, version 1

Entry: `server/handler.mjs` exports `handleReleaseDeck({request,env}, deps)`. The host mounts it at `/api/commerce/release-deck/` and supplies `deps.accountFromRequest(request,db)`, `deps.accountStatus(db,{id})`, `deps.hash(string)` and `deps.limit(db,identity,action,max,seconds)`. Authentication and billing remain the main site's existing system. Errors are `{error,code}` with a corresponding HTTP refusal; no provider payload, token or request body is logged.

Required runtime files: `server/handler.mjs`, `rules.mjs`, `workspaces.mjs`, `crypto.mjs`, `google.mjs`, `operations.mjs`, `maintenance.mjs`. Additive migration: `schema/release-deck.sql`. No existing workspace kind constraint or old table is rebuilt. Main `DB` is used. `SITE_ORIGIN` must be the canonical host. The OAuth redirect is that origin plus `/api/commerce/release-deck/oauth/callback`.

## Configuration and document

`GET config` returns `{configured,googleConfigured,account,limits,providerSetupRequired}`. Storage works without Google credentials. Channel work requires `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, a 32-byte `TOKEN_ENCRYPTION_KEY` encoded as 43-character base64url or 64 hex characters, and `PROVIDER_MAINTENANCE_READY='true'`. The host maps its product-specific secret names. Never inherit an unrelated provider key.

The portable/shared body has exactly:

```text
{schema:1,title,description,episodes:[
 {id,title,description,defaultLanguage,
  localizations:[{language,title,description}],tags,categoryId,
  madeForKids,containsSyntheticMedia,publishAt,
  file:null|{name,size,lastModified,mime,algorithm,fingerprint,duration,width,height}}
]}
```

Episodes use UUIDs. `file.algorithm` is `rd-sha256-tree-4m-v1`; fingerprint is 64 lowercase hex characters. This is a domain-separated all-byte tree identity, not raw full-file SHA-256. Duration is seconds; timestamps are Unix milliseconds, except `publishAt`, which is a UTC ISO string or null. Nullable audience choices and file are accepted in drafts, but approval requires explicit choices and completed media inspection. Provider identities, tokens, URLs and file bytes are not document fields. Limits: 40 episodes, project title120/description2000, video title100, description5000 UTF-8 bytes, localized equivalents, encoded tags500. Angle brackets are rejected in video titles/descriptions. Document size is64KiB free/512KiB paid.

The file identity is pinned user-supplied local evidence. Google does not attest that tree fingerprint. The browser must match it against its actual selected file before sending any bytes.

## Workspaces and review

| Method and relative route | Request | Response |
| --- | --- | --- |
| GET `workspaces` | — | `{workspaces:[summary],account}` |
| POST `workspaces` | `{requestId,body}` | `{workspace}` |
| GET `workspaces/:id` | — | `{workspace}` |
| POST `workspaces/:id` | `{revision,requestId,body}` | `{workspace,savedRevision?}` |
| POST `workspaces/:id/archive` or `restore` | `{}` | `{archived:true}` or `{workspace}` |
| GET `workspaces/:id/history` | — | `{versions:[{revision,savedAt,memberId}]}` |
| GET `workspaces/:id/members` | — | `{members:[{id,memberId,role,joinedAt}]}` |
| POST `workspaces/:id/members` | `{memberId,role:'editor'|'viewer'|'remove'}` | `{ok:true}` |
| GET `workspaces/:id/invites` | — | `{invites:[{id,role,expiresAt,revokedAt,used}]}` |
| POST `workspaces/:id/invites` | `{role:'editor'|'viewer'}` | `{token,expiresAt}` |
| POST `workspaces/:id/invites/:inviteId/revoke` | `{}` | `{ok:true}` |
| POST `invites/join` | `{token}` | `{workspaceId,role}` |
| GET `workspaces/:id/reviews` | — | `{reviews:[{id,revision,memberId,decision,note,createdAt}]}` |
| POST `workspaces/:id/reviews` | `{revision,decision:'comment'|'changes_requested'|'approve',note}` | `{workspace}` |

Workspace fields are `{id,title,ownerId,revision,role,archivedAt,body,approvedRevision,canOperate,createdAt,updatedAt}`. Summary rows omit body. Owners manage membership/archives; editors save and review; viewers read. Only the owner approves channel work. Saves use revision comparison and invalidate approval. The approved body hash and file identity must still match before remote dispatch. Stale writes return409; previous revisions remain separate. Invitation joins are one-use and enforce4/12 members including the owner. Active owned projects share the main3/100 allowance; release insertion/restoration uses the combined count atomically.

## Connections

| Route | Meaning |
| --- | --- |
| POST `oauth/start` `{requestId}` | Returns `{authorizationUrl}` and a separate HttpOnly/Lax callback nonce cookie. |
| GET `oauth/callback` | One-use state, browser nonce, proof-key verifier and original active commerce session are checked. Redirects to a clean UI URL. |
| GET `connections` | Owner connections, with actual channel reads refreshing checked metadata when successful. |
| POST `connections/:id/disconnect` `{}` | Deletes local connection/associated provider data; `{disconnected:true,providerRevoked:boolean}` distinguishes confirmed Google revocation. |

A connection projection is `{id,channelId,title,scopes,expiresAt,checkedAt,updatedAt}`. `expiresAt` is the local retention/revalidation deadline, not a promise about Google's authorization lifetime. Refresh tokens, proof-key verifiers and upload sessions use AES-GCM with independent random nonces and object-bound associated data. No portable export includes them. A callback can succeed without the main Strict cookie, but a logged-out original session cannot be revived through it.

## Uploads and operations

Channel/operation endpoints are owner-only; collaborators do not receive private Google API data by default.

| Route | Request/response |
| --- | --- |
| GET `workspaces/:id/operations` | `{operations:[operation]}` |
| POST `workspaces/:id/uploads` | `{requestId,revision,episodeId,connectionId,fileIdentity,confirmPrivate:true}` → `{operation}` |
| GET `operations/:id` | `{operation}` |
| POST `operations/:id/reconcile` `{}` | Queries the recorded session/video; `{operation}`. No new upload. |
| POST `operations/:id/capability` `{}` | Returns the transient owner transfer grant below. |
| POST `operations/:id/cancel` `{}` | Stops further local grants and forgets the URI. `{operation,remoteDeletion:false}`. |
| POST `operations/:id/restart` | `{requestId,revision,confirmPrivate:true,confirmPossibleDuplicate:true}`; explicit separate attempt only when no videoID is known and the prior attempt is failed/expired/canceled/unknown. |
| POST `operations/:id/metadata` | `{requestId,revision}`; applies approved metadata to the recorded video. |
| POST `operations/:id/publish` | `{requestId,revision,privacyStatus:'private'|'public'|'unlisted',publishAt:null|UTC,confirm:true}` |

Operation: `{id,workspaceId,episodeId,revision,appliedRevision,fileIdentity,status,videoId,offset,totalBytes,processing,action,uncertain,error,createdAt,updatedAt}`. File identity here is `{algorithm,fingerprint,byteLength}`. `error` is nullable text code. States include `initiating,ready,uploading,processing,complete,unknown_init,uncertain,conflict,expired,canceled,failed`.

The capability is `{operationId,fileIdentity,capabilityExpiresAt,uploadUrl,authorization:{accessToken,expiresAt},offset,totalBytes}`. It is private/no-store and must remain in memory. The access token has Google's granted scope; it is not advertised as an upload-only token. No refresh token is returned. The final single SQL admission checks current operation/session identity, uncanceled state, owner/revision/approval, active connection and current commerce session. No asynchronous step occurs between that admission and capability construction.

Browser media bytes go directly to the approved Google resumable URL. Client progress and browser-observed video IDs are not authoritative. The server probes known sessions for an actual308 range or final video response, then reads the owned channel's video processing state. A lost session-creation result is `unknown_init`; no automatic new insert follows. Existing request identities recover their stored operation. Explicit restart acknowledges possible duplication. Cancel cannot recall an already issued Google bearer/session or bytes already received.

Metadata updates read current mutable parts, merge the approved fields while preserving unrelated languages/status values, and send `If-Match`. A412 becomes409 rather than a forced overwrite. The official [update contract](https://developers.google.com/youtube/data/reference/rest/v3/videos/update) defines replacement semantics; the [ETag guide](https://developers.google.com/youtube/data/getting-started#etags) and [global errors](https://developers.google.com/youtube/data/reference/rest/v3/core_errors#PRECONDITION_FAILED) define conditional-write refusal. A lost update response is reconciled by comparing its stored intended fields with a fresh provider read, never by blindly repeating it. Publish requires the currently approved metadata to match the actual remote video. Scheduling uses private status and a future UTC time; the provider remains the authority on eligibility.

## Maintenance and evidence boundary

`server/maintenance.mjs::cleanupExpiredReleaseData(db,now)` is the sole cleanup SQL source. The main hourly Worker imports it. States expire after10minutes; provider connections and operations use29-day deadlines. Operation deletion cascades action payloads/receipts; connection expiry also removes related operations. User-authored workspaces/revisions/reviews are separate. Reads reject expired provider records without pretending that a read-time check alone guarantees physical expiry.

Controlled checks passed53 actual-SQL/provider assertions and6 focused draft validation assertions using the actual SQLite migration, handlers, crypto and fixed provider transports. They cover roles, revision/approval, quota, callback/session binding, encrypted tokens, known-session offsets, no insert replay, owner-only capabilities, conditional metadata preservation, ambiguous-response reconciliation and D1 cleanup. These fixtures are not live Google consent, upload, publication or commercial evidence. The SQL/provider set includes paused cancellation and approval revocation before capability admission, removal of an editor while its request body is paused, explicit separate restart and lost-initialization replay with no second insert. The6 validation checks cover nullable drafts, UTF-8 size, angle brackets, localized metadata, identity algorithms and rejection of browser-supplied video IDs. Host/native/live-provider acceptance is separate.

### Host-session integration follow-up

The existing host stores member sessions in `commerce_sessions.session_hash`, not `token_hash`. OAuth start, callback state claiming, callback connection admission and the final upload-capability admission now use that exact host column. The API adapter continues to import the host authentication/entitlement/hash/rate helpers; it does not introduce another session schema.

The original53 component checks used a reduced commerce fixture with a different session-column name and did not establish compatibility with the actual host authentication tables. This follow-up aligned only that fixture's session table, authentication query and logout query with `session_hash`. The existing53 actual-SQL/controlled-provider assertions then passed once against the corrected production source. Production was corrected to match the host, not changed to accommodate the test. The unchanged6 pure document-validation checks were not repeated; their earlier result remains separate.

A separate8-check integration fixture imported the real BinSite `schema/commerce.sql` and its actual `accountFromRequest`, `accountStatus`, `hash` and `limit` exports. Google transport remained controlled, and only the unrelated legacy-workspace count table was a reduced fixture. Before correction, OAuth start failed its host-session assertion. After correction, all8 checks passed: host authentication, OAuth start, nonce-bound callback without the Strict member cookie, connection insertion, active-session capability admission, a second pending OAuth flow, session revocation at final capability admission and rejection of its later callback without contacting Google. Current follow-up evidence is53+8 checks; the6 unchanged document validations retain their original result.

This is local SQLite/request evidence. It does not claim deployed cron execution, real Google consent, media transfer or publication.
