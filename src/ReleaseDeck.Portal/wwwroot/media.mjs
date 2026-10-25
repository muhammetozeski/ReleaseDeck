import { requireMedia, registerMedia, forgetMedia, allMediaIds, identityAlgorithm, fingerprintChunkBytes, sameIdentity } from './media-state.mjs';

const previewBindings = new WeakMap(), dropZones = new Map();
const rootDomain = new TextEncoder().encode('ReleaseDeck file fingerprint v1\0');

function descriptor(entry) { return { clientFileId: entry.clientFileId, name: entry.file.name, size: entry.file.size, lastModified: entry.file.lastModified || 0, mimeType: entry.file.type || 'application/octet-stream' }; }
function cancelled(signal) { if (signal.aborted) throw new DOMException('File inspection cancelled.', 'AbortError'); }
function registerFiles(list) {
  const values = [...list];
  if (values.length > 40) throw new Error('Select up to 40 episode files at once.');
  const selected = [];
  try { for (const file of values) selected.push(registerMedia(file)); return selected; }
  catch (error) { for (const item of selected) releaseFile(item.clientFileId); throw error; }
}
export function selectFiles(input) {
  if (!input?.files) throw new Error('Choose files using the video file control.');
  try { return registerFiles(input.files); } finally { input.value = ''; }
}
export async function loadSamples() {
  const files = [];
  for (const name of ['sample-overview.mp4', 'sample-demonstration.mp4', 'sample-wrap-up.mp4']) {
    const url = new URL('./samples/' + name, import.meta.url);
    if (url.origin !== location.origin) throw new Error('Sample clips must come from this site.');
    const response = await fetch(url, { credentials: 'omit', cache: 'no-cache', redirect: 'error' });
    if (!response.ok || !response.body) throw new Error('A sample clip could not be loaded.');
    const reader = response.body.getReader(), chunks = []; let size = 0;
    while (true) { const part = await reader.read(); if (part.done) break; size += part.value.byteLength; if (size > 16 * 1024 * 1024) { await reader.cancel(); throw new Error('The sample clip exceeds its size limit.'); } chunks.push(part.value); }
    files.push(new File(chunks, name, { type: 'video/mp4', lastModified: 0 }));
  }
  return registerFiles(files);
}
export function bindDropZone(element, receiver, method = 'OnMediaDropped', errorMethod = 'OnMediaError') {
  if (!element?.addEventListener || !receiver?.invokeMethodAsync) throw new Error('The file drop area is unavailable.');
  const id = crypto.randomUUID();
  const over = event => { event.preventDefault(); if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy'; };
  const drop = async event => {
    event.preventDefault(); event.stopPropagation();
    let selected = [];
    try { selected = registerFiles(event.dataTransfer?.files || []); await receiver.invokeMethodAsync(method, selected); }
    catch { for (const item of selected) releaseFile(item.clientFileId); try { await receiver.invokeMethodAsync(errorMethod, 'The files could not be added. Choose up to 40 local episode files.'); } catch {} }
  };
  element.addEventListener('dragover', over); element.addEventListener('drop', drop);
  dropZones.set(id, { element, over, drop });
  return id;
}
export function unbindDropZone(id) {
  const binding = dropZones.get(id); if (!binding) return;
  binding.element.removeEventListener('dragover', binding.over); binding.element.removeEventListener('drop', binding.drop); dropZones.delete(id);
}
function waitMedia(video, event, setup, signal) {
  return new Promise((resolve, reject) => {
    const cleanup = () => { clearTimeout(timer); video.removeEventListener(event, done); video.removeEventListener('error', failed); signal.removeEventListener('abort', aborted); };
    const done = () => { cleanup(); resolve(); };
    const failed = () => { cleanup(); reject(new Error('This browser could not decode the selected video.')); };
    const aborted = () => { cleanup(); reject(new DOMException('File inspection cancelled.', 'AbortError')); };
    const timer = setTimeout(() => { cleanup(); reject(new Error('Video information could not be read. Try another supported video file.')); }, 15000);
    video.addEventListener(event, done, { once: true }); video.addEventListener('error', failed, { once: true }); signal.addEventListener('abort', aborted, { once: true });
    try { cancelled(signal); setup(); } catch (error) { cleanup(); reject(error); }
  });
}
export async function inspectFile(id) {
  const entry = requireMedia(id);
  if (entry.metadata) return { ...entry.metadata };
  if (entry.metadataPromise) return entry.metadataPromise;
  entry.metadataPromise = (async () => {
    const video = document.createElement('video'); entry.probe = video; video.preload = 'metadata'; video.muted = true; video.playsInline = true;
    let result;
    try {
      await waitMedia(video, 'loadedmetadata', () => { video.src = entry.url; video.load(); }, entry.lifecycle.signal);
      if (!Number.isFinite(video.duration) || video.duration <= 0 || !Number.isInteger(video.videoWidth) || video.videoWidth <= 0 || !Number.isInteger(video.videoHeight) || video.videoHeight <= 0) throw new Error('This file has no readable video duration or resolution.');
      result = { ...descriptor(entry), durationMs: video.duration * 1000, width: video.videoWidth, height: video.videoHeight, error: null };
    } catch (error) {
      if (entry.lifecycle.signal.aborted) throw new DOMException('File inspection cancelled.', 'AbortError');
      result = { ...descriptor(entry), durationMs: null, width: null, height: null, error: error.message };
    } finally {
      video.pause(); video.removeAttribute('src'); video.load(); if (entry.probe === video) entry.probe = null;
    }
    entry.metadata = result; return { ...result };
  })();
  return entry.metadataPromise;
}
export function clearPreview(video) {
  if (!video) {
    for (const id of allMediaIds()) for (const preview of requireMedia(id).previews) if (preview.isConnected === false) clearPreview(preview);
    return;
  }
  const id = previewBindings.get(video);
  if (id) { try { requireMedia(id).previews.delete(video); } catch {} previewBindings.delete(video); }
  if (video) { video.pause(); video.removeAttribute('src'); video.load(); }
}
export function mountPreview(video, id) {
  const entry = requireMedia(id);
  if (!video || String(video.tagName).toLowerCase() !== 'video') throw new Error('The video preview is unavailable.');
  if (previewBindings.get(video) === id) return;
  clearPreview(video); previewBindings.set(video, id); entry.previews.add(video); video.preload = 'metadata'; video.src = entry.url; video.load();
}
export function previewTime(video) {
  const id = video && previewBindings.get(video);
  if (!id || !Number.isFinite(video.currentTime) || video.currentTime < 0) throw new Error('Choose a video in the preview before capturing a frame.');
  requireMedia(id); return video.currentTime * 1000;
}
export async function thumbnail(id, timeMs = 0, maxWidth = 640) {
  const entry = requireMedia(id);
  const execute = async () => {
    const metadata = await inspectFile(id);
    if (metadata.error) throw new Error(metadata.error);
    const video = document.createElement('video'), signal = entry.lifecycle.signal; entry.probe = video; video.muted = true; video.playsInline = true;
    try {
    await waitMedia(video, 'loadeddata', () => { video.preload = 'auto'; video.src = entry.url; video.load(); }, signal);
    const seconds = Math.max(0, Math.min(Number.isFinite(timeMs) ? timeMs / 1000 : 0, Math.max(0, video.duration - 0.001)));
    if (Math.abs(video.currentTime - seconds) > 0.001) await waitMedia(video, 'seeked', () => { video.currentTime = seconds; }, signal);
    cancelled(signal);
    const widthLimit = Math.max(64, Math.min(1280, Number.isFinite(maxWidth) ? Math.floor(maxWidth) : 640));
    const scale = Math.min(1, widthLimit / metadata.width, 720 / metadata.height), canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(metadata.width * scale)); canvas.height = Math.max(1, Math.round(metadata.height * scale));
    const context = canvas.getContext('2d'); if (!context) throw new Error('Thumbnail rendering is unavailable.');
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    const dataUrl = canvas.toDataURL('image/jpeg', 0.86);
    if (!dataUrl.startsWith('data:image/jpeg;base64,') || dataUrl.length > 3 * 1024 * 1024) throw new Error('The thumbnail could not be rendered.');
    return { clientFileId: id, dataUrl, width: canvas.width, height: canvas.height, timeMs: video.currentTime * 1000 };
    } finally { video.pause(); video.removeAttribute('src'); video.load(); if (entry.probe === video) entry.probe = null; }
  };
  const pending = entry.thumbnailQueue.catch(() => {}).then(execute); entry.thumbnailQueue = pending; return pending;
}
const digest = async bytes => new Uint8Array(await crypto.subtle.digest('SHA-256', bytes));
function uint64(view, offset, value) { view.setBigUint64(offset, BigInt(value), false); }
async function rootFingerprint(file, signal, progress) {
  const frontier = []; let leaves = 0;
  for (let offset = 0; offset < file.size; offset += fingerprintChunkBytes) {
    cancelled(signal);
    const bytes = new Uint8Array(await file.slice(offset, Math.min(file.size, offset + fingerprintChunkBytes)).arrayBuffer());
    cancelled(signal);
    const leaf = new Uint8Array(bytes.length + 5); leaf[0] = 0; new DataView(leaf.buffer).setUint32(1, bytes.length, false); leaf.set(bytes, 5);
    let node = await digest(leaf), level = 0; leaves++;
    while (frontier[level]) {
      const pair = new Uint8Array(65); pair[0] = 1; pair.set(frontier[level], 1); pair.set(node, 33); node = await digest(pair); frontier[level] = null; level++;
    }
    frontier[level] = node; cancelled(signal); await progress(Math.min(file.size, offset + bytes.length));
  }
  const peaks = [];
  for (let level = frontier.length - 1; level >= 0; level--) if (frontier[level]) peaks.push({ level, bytes: frontier[level] });
  const header = new Uint8Array(1 + rootDomain.length + 4 + 8 + 8 + 1 + peaks.length * 33); header[0] = 2; header.set(rootDomain, 1);
  const view = new DataView(header.buffer); let cursor = 1 + rootDomain.length;
  view.setUint32(cursor, fingerprintChunkBytes, false); cursor += 4; uint64(view, cursor, file.size); cursor += 8; uint64(view, cursor, leaves); cursor += 8; header[cursor++] = peaks.length;
  for (const peak of peaks) { header[cursor++] = peak.level; header.set(peak.bytes, cursor); cursor += 32; }
  cancelled(signal); return [...await digest(header)].map(value => value.toString(16).padStart(2, '0')).join('');
}
export async function fingerprintFile(id, receiver = null, method = 'OnFingerprintProgress') {
  const entry = requireMedia(id);
  if (entry.identity) return { clientFileId: id, ...entry.identity };
  if (entry.hashJob) return entry.hashJob.promise;
  const controller = new AbortController(), job = { controller, promise: null }; let lastProgress = 0;
  job.promise = (async () => {
    const fingerprint = await rootFingerprint(entry.file, controller.signal, async bytesRead => {
      if (receiver && (bytesRead === entry.file.size || performance.now() - lastProgress > 100)) {
        lastProgress = performance.now();
        try { await receiver.invokeMethodAsync(method, { clientFileId: id, state: 'hashing', bytesRead, totalBytes: entry.file.size }); }
        catch { controller.abort(); cancelled(controller.signal); }
      }
    });
    cancelled(controller.signal); requireMedia(id);
    entry.identity = { algorithm: identityAlgorithm, fingerprint, size: entry.file.size };
    return { clientFileId: id, ...entry.identity };
  })();
  entry.hashJob = job;
  try { return await job.promise; } finally { if (entry.hashJob === job) entry.hashJob = null; }
}
export function cancelFingerprint(id) { const entry = requireMedia(id); entry.hashJob?.controller.abort(); }
export async function matchIdentity(id, expected) { const actual = await fingerprintFile(id); return sameIdentity(actual, expected); }
export function releaseFile(id) {
  const entry = forgetMedia(id); if (!entry) return;
  for (const video of entry.previews) clearPreview(video);
  entry.previews.clear();
  if (entry.probe) { entry.probe.pause(); entry.probe.removeAttribute('src'); entry.probe.load(); }
  URL.revokeObjectURL(entry.url);
}
export function dispose() { for (const id of allMediaIds()) releaseFile(id); for (const id of [...dropZones.keys()]) unbindDropZone(id); }
export function download(name, text, type = 'application/json') {
  if (typeof text !== 'string') throw new Error('The export is not text.');
  const url = URL.createObjectURL(new Blob([text], { type })), link = document.createElement('a');
  try { link.href = url; link.download = String(name).replace(/[<>:"/\\|?*\u0000-\u001f]/g, '_'); document.body.append(link); link.click(); }
  finally { link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000); }
}
