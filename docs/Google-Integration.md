# Google integration and verified boundaries

This document separates official protocol support, the implemented adapter, controlled fixtures and owner setup. No personal Google credentials were inspected and no live upload/publication was performed during implementation.

## Authorization

The adapter requests only `https://www.googleapis.com/auth/youtube.force-ssl`, which is listed for [video insert](https://developers.google.com/youtube/data/reference/rest/v3/videos/insert) and [update](https://developers.google.com/youtube/data/reference/rest/v3/videos/update). Owned channel identity is read with [channels.list mine=true](https://developers.google.com/youtube/data/reference/rest/v3/channels/list); recorded video IDs use [videos.list](https://developers.google.com/youtube/data/reference/rest/v3/videos/list). There is no assumed bulk listing/import of every private video.

The [web-server OAuth flow](https://developers.google.com/identity/protocols/oauth2/web-server) uses a registered exact HTTPS redirect, code exchange, offline access and refresh/revocation endpoints. Release Deck adds a random one-use state, S256 proof-key challenge and a browser-bound Lax callback cookie. Google's [official OAuth client source](https://github.com/googleapis/google-auth-library-nodejs/blob/main/src/auth/oauth2client.ts) also documents pairing the S256 challenge with the code verifier. State is tied to the original live commerce session. Cross-site return does not depend on the main Strict cookie. Codes are consumed server-side and removed from the final UI URL. Credentials are AES-GCM encrypted with object-specific associated data; provider credentials are not accepted from clients.

Required owner configuration is a dedicated OAuth client/secret, registered production redirect, the main site's dedicated encryption key and confirmed hourly maintenance. Neither existence of an adapter nor a fixture proves consent-screen verification, quota, audit outcome or publication eligibility. Configuration remains disabled until its actual prerequisites are present.

## Direct resumable upload

The [current resumable protocol](https://developers.google.com/youtube/data/guides/using_resumable_upload_protocol) defines session creation, the returned Location, byte PUTs,308 range queries and final resource responses. Release Deck creates a private session server-side, encrypts that URI and grants it only transiently to the owner. Bytes never pass through Cloudflare. Chunking follows the documented256KiB multiples; the browser controls Content-Length. No upload URL is client-selectable.

The official [JavaScript examples](https://github.com/youtube/api-samples/blob/master/javascript/README.md) describe resumable XHR/CORS and upload/processing reads; that repository is archived, so it is not a live production acceptance result. The media agent separately checked public OPTIONS without credentials. A browser may not expose a308 Range header; in that case the client uses this server's authenticated status query rather than guessing offset zero. Any actual OAuth/channel/project/browser restrictions still need owner-authorized acceptance after setup.

Sessions are finite-lived. Known sessions are queried before transfer resumes. Expired sessions, ambiguous initialization and known final video IDs are distinct states. No new upload is silently substituted when the first initialization response is lost. Explicit separate-attempt confirmation is required to restart an unresolved attempt.

## Safe metadata and visibility changes

[videos.update](https://developers.google.com/youtube/data/reference/rest/v3/videos/update) replaces mutable fields in selected parts. The adapter first reads the current owned video, preserves the supported mutable status fields and unrelated localized languages, then merges only intended approved values. It never sends read-only snippet fields back. [ETags](https://developers.google.com/youtube/data/getting-started#etags) with `If-Match` prevent replacing a newer provider version; [HTTP412](https://developers.google.com/youtube/data/reference/rest/v3/core_errors#PRECONDITION_FAILED) becomes an explicit conflict. No wildcard precondition or force overwrite is used.

The [resource contract](https://developers.google.com/youtube/data/reference/rest/v3/videos) describes title/description constraints, encoded tag length, processing fields and owner file details. Backend and preflight enforce title/description angle-bracket exclusions and the description's5000-byte UTF-8 limit. Publish remains a separate owner action. A future schedule requires a private video; the adapter does not assert that a video has never previously been public or that every channel/project is eligible. Provider refusal is retained without inventing success. No batch is presented as atomic.

## Data lifetime and disconnect

The [developer policy III.E.4](https://developers.google.com/youtube/terms/developer-policies) distinguishes authorized/non-authorized data and generally requires other stored API metadata to be refreshed or deleted within30days. Release Deck uses29-day deadlines for its provider cache, encrypted session/operation records and connection metadata. A real successful channel/video read can renew the relevant deadline; merely opening a UI cannot. An hourly D1-only host Worker deletes expired records and cascades action payloads. User-authored release drafts/history are kept separately.

Disconnect attempts immediate provider revocation, then removes local credentials and associated API data even when the revocation response is uncertain. The response distinguishes confirmed revocation from local disconnection. Users can also revoke through [Google account permissions](https://security.google.com/settings/security/permissions). Main account deletion cascades this product's private rows. There is no YouTube audiovisual download, cache, transcoding, analytics or audience forecast in this adapter.

## Evidence

Official documentation above supports the intended protocol. The53 actual-SQL/controlled-provider assertions and6 focused document-validation checks exercise the production handlers and AES-GCM with disposable credentials, not a personal channel. The prefix/main-host adapter, deployed maintenance schedule, real OAuth client consent, browser CORS transfer and actual provider writes are separate acceptance gates. Owner client credentials remain unset during these fixtures; public copy must not claim successful uploads, verified permissions, user demand or income.
