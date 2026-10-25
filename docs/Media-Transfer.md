# Media and transfer bridge

## Source ownership

The browser bridge is `src/ReleaseDeck.Portal/wwwroot/media.mjs`, `media-state.mjs`, and `upload.mjs`. It imports no external package. The portal owns its controls, project JSON, downloads, storage, and account actions. The server owns channel connections, approval, upload operations, capabilities, receipts, and processing state.

Local selection, preview, frame capture, and file identification require neither an account nor a Google key. Selecting a file or loading a sample never starts an upload. A `clientFileId`, object URL, File object, thumbnail data URL, access token, and upload session URL are never portable project fields. The bridge does not write localStorage or send logs.

## Local media interface

Import `./media.mjs` relative to the published app base.

| Export | Result or effect |
| --- | --- |
| `selectFiles(inputElement)` | Captures the actual File objects, resets the input, returns descriptors; maximum 40 per selection. |
| `bindDropZone(element, receiver, method='OnMediaDropped', errorMethod='OnMediaError')` | Prevents drop navigation; the first callback receives descriptors, the error callback receives a short message. Returns an unbinding ID. |
| `unbindDropZone(id)` | Removes those event handlers. |
| `loadSamples()` | Fetches only the three packaged same-origin MP4 clips relative to the module; returns descriptors through the same file registry. Maximum 16 MiB per packaged sample. |
| `inspectFile(clientFileId)` | Native video metadata: descriptor plus `durationMs`, `width`, `height`, and `error`. Unsupported media returns null dimensions/duration and an error; it does not acquire invented metadata. |
| `mountPreview(videoElement, clientFileId)` | Binds the actual local object URL; replaces the prior preview. |
| `clearPreview(videoElement)` | Pauses and removes the prior source. |
| `previewTime(videoElement)` | Current bound preview time in milliseconds; refuses an unbound preview. |
| `thumbnail(clientFileId, timeMs=0, maxWidth=640)` | Seeks a separate native video element and draws its frame to a canvas. Returns `{clientFileId,dataUrl,width,height,timeMs}` with JPEG pixels; at most 1280×720 and 3 MiB encoded output. |
| `fingerprintFile(clientFileId, receiver?, method='OnFingerprintProgress')` | Returns `{clientFileId,algorithm,fingerprint,size}` after all bytes have been read. Progress is `{clientFileId,state:'hashing',bytesRead,totalBytes}`. |
| `cancelFingerprint(clientFileId)` | Aborts the hash without storing a partial identity. |
| `matchIdentity(clientFileId, expected)` | Computes or reuses the complete identity; compares algorithm, fingerprint, and `byteLength` or `size`. |
| `releaseFile(clientFileId)` / `dispose()` | Abort pending work, clear previews, revoke object URLs, and remove retained files/listeners. |

Descriptors are `{clientFileId,name,size,lastModified,mimeType}`. Portable metadata uses `mime` and duration in seconds; the C# adapter converts milliseconds to seconds. A decoding error does not prevent computing the complete byte identity. A thumbnail is a separate local output, not evidence that YouTube accepted the video. UI consumers must retain their selected-file/episode context across awaited inspection and frame capture.

`download(name,text,type='application/json')` is an optional text download helper; normal portal exports use its own app module.

## Complete-file identity

The algorithm name is **`rd-sha256-tree-4m-v1`**. Its fingerprint is a lowercase, 64-character hexadecimal tree root, not a flat SHA-256 digest of the file. File names, timestamps, and media metadata are not inputs to this identity.

All integers below are unsigned and big endian. Source chunks are 4,194,304 bytes except the final chunk. Every byte participates, including the final partial chunk.

1. Each leaf is `SHA256(0x00 || uint32(chunkLength) || chunkBytes)`.
2. Adjacent equal-height subtrees combine as `SHA256(0x01 || leftDigest || rightDigest)`. Binary carry retains at most one subtree digest per height.
3. Remaining peaks are ordered by descending height. The final root is SHA-256 of `0x02 || UTF8("ReleaseDeck file fingerprint v1\0") || uint32(4194304) || uint64(fileSize) || uint64(leafCount) || uint8(peakCount) || [uint8(height) || digest]` for each peak. The null in the domain is one zero byte.

A zero-byte file has no peaks and a defined root, but cannot be uploaded. Identification reads one slice at a time and retains logarithmically many 32-byte digests; it does not read the entire video into an ArrayBuffer or retain all leaves. The leaf input and Web Crypto may copy the current chunk. Native video decoding has separate browser-managed buffers; no exact process-memory bound is claimed.

After reload, the registry and bytes are gone. Importing project metadata alone cannot make a video attached or resumable. The user selects the file again and computes its complete identity. A different last byte refuses rebind, even if the name, size, and timestamp are unchanged.

## Transfer interface and authority

Import `./upload.mjs`. `start(operationId,clientFileId,receiver?,method='OnUploadProgress')` and `resume(operationId,clientFileId,receiver?,method='OnUploadProgress')` transfer only a previously identified selected file for an existing operation. They do not create a new remote video or retry an uncertain creation. `pause(operationId)` stops this device; an interrupted data request is reconciled when possible. `cancel(operationId)` also calls the owner cancellation endpoint. `dispose()` stops local transfers without claiming remote cancellation or deletion.

Callback and return objects contain only:

```json
{"operationId":"opaque UUID","phase":"uploading","confirmedBytes":0,"totalBytes":1,"message":"Sending the selected video."}
```

Phases are `checking`, `reconciling`, `uploading`, `paused`, `uncertain`, `uploaded`, `cancelled`, or `error`. They are progress labels. The operation returned by the application server is the saved receipt and processing state. An uploaded label requires a server-observed video ID; a browser HTTP 201 by itself is insufficient.

Control requests are same-origin, no-store POSTs to `/api/commerce/release-deck/operations/{id}/{reconcile|capability|cancel}`. Before any data PUT, the bridge reconciles and obtains an owner capability whose file identity, total bytes, expiry, and destination must match. Only `https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&upload_id={opaque-session-id}` is admitted. No application cookies or referrer accompany the cross-origin PUT. Authorization/session URL remain inside the JS transfer closure and are cleared on stop or completion; neither is passed to a .NET callback.

The capability endpoint performs the server-side session query and authorization checks. Its DTO includes `operationId`, `fileIdentity:{algorithm,fingerprint,byteLength}`, `offset`, `totalBytes`, `capabilityExpiresAt`, `uploadUrl`, and `authorization:{accessToken,expiresAt}`. The returned Google bearer token carries its granted Google scope; it is not described as an upload-only token. Expiry requires fresh authorization. Cancellation prevents subsequent app capabilities; it cannot recall a capability already delivered, revoke a bearer token immediately, or delete bytes already sent to Google.

## Google protocol and observed CORS

The [official resumable protocol](https://developers.google.com/youtube/data/guides/using_resumable_upload_protocol) defines fixed chunks in multiples of 256 KiB, `Content-Range`, 308 acknowledgements, and empty PUT status queries. This bridge sends fixed 4 MiB Blob slices with a shorter final slice. An exposed `Range: bytes=0-N` confirms offset `N+1`. Missing, malformed, unexpectedly advanced, or lost acknowledgements trigger a server query; no client byte count is substituted for the returned position. A failed data request is queried once and paused, rather than automatically replayed. The next explicit resume starts from fresh server evidence.

The [official implementation guide](https://developers.google.com/youtube/data/guides/implementation/videos) links a browser CORS upload example and separates upload reception from processing. The linked [archived browser sample](https://github.com/youtube/api-samples/blob/master/javascript/cors_upload.js) was inspected; the bridge is its own implementation using the current protocol, not a copied sample or a provider SDK.

On 2026-10-25, an unauthenticated OPTIONS request to the official upload initiation endpoint, with the BinSite origin and requested PUT headers `authorization,content-type,content-range`, returned HTTP 200. It allowed that origin, PUT, and those three headers. That observation proves preflight acceptance only. It did not exercise an authorized session, expose its Range response, upload a file, create a video, or establish processing success. If a browser cannot read Range, the owner server probe supplies the next offset.

## Verification evidence and limits

Node syntax checks passed for all three modules. The disposable consumer script at `C:\C\ReleaseDeckMedia\consumer-checks.mjs` completed **125 assertions** against the production modules. It independently constructs the tree reference, denies whole-file ArrayBuffer reads, checks every slice is at most 4 MiB, checks tail changes, renamed rebind, cancellation/restart and released IDs, and reads the three real packaged sample files through `loadSamples()` and complete identification.

Controlled transport responses cover fixed chunk sizes and ranges, missing/malformed Range, lost acknowledgements, wrong host/file refusal before bytes, canceled operations, callback data excluding credentials, explicit pause, file release, reload/reselect refusal, and HTTP 201 without a confirmed receipt. Consumer video/canvas stubs check metadata units, seeking, output dimensions, and cleanup. Those are JS contract checks, not a native codec, playback, pixel-quality, or live authorized Google upload test. No owner channel, private video, Google access token, or provider credential was used by these checks.
