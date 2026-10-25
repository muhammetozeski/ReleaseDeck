import { requireMedia, normalizedIdentity, sameIdentity } from './media-state.mjs';

const prefix = '/api/commerce/release-deck/operations/';
const chunkBytes = 4 * 1024 * 1024;
const jobs = new Map();
function operationId(value) { if (!/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/.test(value || '')) throw new Error('Choose a saved upload operation.'); return value; }
function publicState(job) { return { operationId: job.id, phase: job.phase, confirmedBytes: job.offset, totalBytes: job.total, message: job.message }; }
async function emit(job, phase, message = '') {
  job.phase = phase; job.message = message;
  if (job.receiver) {
    try { await job.receiver.invokeMethodAsync(job.method, publicState(job)); }
    catch { job.control = 'pause'; job.controller?.abort(); job.capability = null; job.receiver = null; }
  }
  return publicState(job);
}
async function api(id, action) {
  const response = await fetch(prefix + operationId(id) + '/' + action, { method: 'POST', credentials: 'same-origin', cache: 'no-store', redirect: 'error', headers: { 'Content-Type': 'application/json' }, body: '{}' });
  if (!response.ok) {
    const messages = { 401: 'Sign in again before continuing the upload.', 403: 'Only the authorized workspace owner can upload.', 409: 'This upload changed. Refresh its operation before continuing.', 429: 'Wait before resuming this upload.', 503: 'Uploading is unavailable. Your local release remains available.' };
    throw new Error(messages[response.status] || 'The upload action could not be confirmed. Refresh its operation.');
  }
  try { return await response.json(); } catch { throw new Error('The upload action returned an unreadable result. Refresh its operation.'); }
}
function expiry(value) { const result = typeof value === 'number' ? value : Date.parse(value); return Number.isFinite(result) ? result : 0; }
function offset(value, total) { if (!Number.isSafeInteger(value) || value < 0 || value > total) throw new Error('The upload offset could not be confirmed. Reconcile before continuing.'); return value; }
function validateCapability(value, job, entry) {
  if (value?.operationId !== job.id || value.totalBytes !== entry.file.size || !entry.identity || !sameIdentity(entry.identity, value.fileIdentity)) throw new Error('The selected file does not match this approved upload. Reselect and identify the correct video.');
  const url = new URL(value.uploadUrl);
  if (url.protocol !== 'https:' || url.hostname !== 'www.googleapis.com' || url.port || url.username || url.password || url.hash || url.pathname !== '/upload/youtube/v3/videos' || url.searchParams.get('uploadType') !== 'resumable' || !url.searchParams.get('upload_id')) throw new Error('The upload destination was refused. Refresh the operation.');
  const token = value.authorization?.accessToken;
  const expiresAt = Math.min(expiry(value.capabilityExpiresAt), expiry(value.authorization?.expiresAt));
  if (typeof token !== 'string' || !token || token.length > 16000 || /[\r\n]/.test(token) || expiresAt <= Date.now() + 1000) throw new Error('Upload authorization expired. Resume to obtain fresh authorization.');
  return { url: url.href, token, expiresAt, offset: offset(value.offset, entry.file.size) };
}
function checkOperation(value, job, entry) {
  const operation = value?.operation;
  if (!operation || operation.id !== job.id || !sameIdentity(entry.identity, operation.fileIdentity) || operation.totalBytes !== entry.file.size) throw new Error('The selected file does not match this saved upload operation.');
  job.total = entry.file.size; job.offset = offset(operation.offset, job.total);
  return operation;
}
async function reconcile(job, entry) {
  await emit(job, 'reconciling', 'Checking the saved upload.');
  return checkOperation(await api(job.id, 'reconcile'), job, entry);
}
function rangeOffset(header, total) {
  if (header === null) return null;
  const match = /^bytes=0-(\d+)$/.exec(header.trim());
  if (!match) throw new Error('Google returned an unreadable upload range. Reconcile before continuing.');
  return offset(Number(match[1]) + 1, total);
}
function alreadyReceived(operation) { return typeof operation.videoId === 'string' && operation.videoId.length > 0; }
async function settleInterruption(job, entry) {
  job.capability = null;
  if (job.control === 'cancel') return publicState(job);
  try {
    const operation = await reconcile(job, entry);
    if (alreadyReceived(operation)) return emit(job, 'uploaded', 'Video received. Refresh the operation for its processing status.');
    if (operation.status === 'canceled') return emit(job, 'cancelled', 'Upload cancelled. Previously sent bytes may remain at Google.');
    return emit(job, 'paused', 'Upload paused. Resume to continue from the confirmed offset.');
  } catch { return emit(job, 'uncertain', 'The upload offset is unknown. Refresh and reconcile before resuming.'); }
}
async function execute(job) {
  let entry;
  try {
    entry = requireMedia(job.fileId);
    if (!entry.identity || entry.file.size <= 0) throw new Error('Identify the selected video before uploading.');
    normalizedIdentity(entry.identity); job.total = entry.file.size;
    job.onRelease = () => { job.control = 'pause'; job.controller?.abort(); job.capability = null; };
    entry.lifecycle.signal.addEventListener('abort', job.onRelease, { once: true });
    const initial = await reconcile(job, entry);
    if (alreadyReceived(initial)) return emit(job, 'uploaded', 'Video received. Refresh the operation for its processing status.');
    if (initial.status === 'canceled') return emit(job, 'cancelled', 'This upload was cancelled.');
    if (initial.uncertain || initial.status === 'unknown_init') return emit(job, 'uncertain', 'Reconcile the existing operation before trying another upload.');
    while (job.offset < job.total) {
      if (job.control) return emit(job, job.control === 'cancel' ? 'cancelled' : 'paused', 'Transfer stopped on this device.');
      if (!job.capability || job.capability.expiresAt <= Date.now() + 5000) {
        const capability = await api(job.id, 'capability');
        if (job.control) continue;
        entry = requireMedia(job.fileId); job.capability = validateCapability(capability, job, entry); job.offset = job.capability.offset;
      }
      if (job.offset === job.total) break;
      const start = job.offset, end = Math.min(start + chunkBytes, job.total), active = job.capability;
      job.controller = new AbortController();
      await emit(job, 'uploading', 'Sending the selected video.');
      if (job.control) continue;
      let response;
      try {
        job.sent = true;
        response = await fetch(active.url, { method: 'PUT', mode: 'cors', credentials: 'omit', cache: 'no-store', redirect: 'error', referrerPolicy: 'no-referrer', signal: job.controller.signal, headers: { Authorization: 'Bearer ' + active.token, 'Content-Type': entry.file.type || 'application/octet-stream', 'Content-Range': 'bytes ' + start + '-' + (end - 1) + '/' + job.total }, body: entry.file.slice(start, end) });
      } catch { return settleInterruption(job, entry); }
      job.controller = null;
      if (job.control === 'cancel') { response.body?.cancel().catch(() => {}); return publicState(job); }
      if (response.status === 200 || response.status === 201) {
        response.body?.cancel().catch(() => {});
        const operation = await reconcile(job, entry);
        return alreadyReceived(operation) ? emit(job, 'uploaded', 'Video received. Refresh the operation for its processing status.') : emit(job, 'uncertain', 'Google accepted the request; reconcile its saved video before continuing.');
      }
      if (response.status !== 308) { response.body?.cancel().catch(() => {}); return settleInterruption(job, entry); }
      let next;
      try { next = rangeOffset(response.headers.get('Range'), job.total); }
      catch { return settleInterruption(job, entry); }
      response.body?.cancel().catch(() => {});
      if (next === null || next > end) {
        const operation = await reconcile(job, entry);
        if (alreadyReceived(operation)) return emit(job, 'uploaded', 'Video received. Refresh the operation for its processing status.');
        if (operation.status === 'canceled') return emit(job, 'cancelled', 'This upload was cancelled.');
        next = job.offset;
      }
      if (next <= start) return emit(job, 'uncertain', 'No further bytes were confirmed. Reconcile before resuming.');
      job.offset = next;
      await emit(job, 'uploading', 'Bytes confirmed by Google.');
      if (response.headers.get('Retry-After')) return emit(job, 'paused', 'Google requested a wait. Resume later from the confirmed offset.');
    }
    const final = await reconcile(job, entry);
    return alreadyReceived(final) ? emit(job, 'uploaded', 'Video received. Refresh the operation for its processing status.') : emit(job, 'uncertain', 'All bytes were acknowledged. Reconcile the saved video before continuing.');
  } catch (error) { return emit(job, job.sent ? 'uncertain' : 'error', error instanceof TypeError ? 'The upload connection could not be confirmed. Refresh its operation.' : error.message); }
  finally {
    if (entry && job.onRelease) entry.lifecycle.signal.removeEventListener('abort', job.onRelease);
    job.capability = null; job.controller = null; job.active = false;
  }
}
export function start(id, clientFileId, receiver = null, method = 'OnUploadProgress') {
  id = operationId(id);
  const existing = jobs.get(id);
  if (existing?.active) { if (existing.fileId !== clientFileId) throw new Error('Pause this upload before selecting a different file.'); return existing.promise; }
  const job = { id, fileId: clientFileId, receiver, method, phase: 'checking', message: '', offset: 0, total: 0, capability: null, controller: null, control: '', active: true, sent: false, onRelease: null, promise: null };
  jobs.set(id, job); job.promise = execute(job); return job.promise;
}
export const resume = start;
export async function pause(id) {
  const job = jobs.get(operationId(id));
  if (!job) return { operationId: id, phase: 'paused', confirmedBytes: 0, totalBytes: 0, message: 'Select the matching local file before resuming.' };
  job.control = 'pause'; job.controller?.abort(); job.capability = null;
  return emit(job, 'paused', 'Pausing the transfer. Its offset will be reconciled.');
}
export async function cancel(id) {
  id = operationId(id); const job = jobs.get(id);
  if (job) { job.control = 'cancel'; job.controller?.abort(); job.capability = null; }
  try { await api(id, 'cancel'); return job ? emit(job, 'cancelled', 'Upload cancelled. Previously sent bytes may remain at Google.') : { operationId: id, phase: 'cancelled', confirmedBytes: 0, totalBytes: 0, message: 'Upload cancelled. Previously sent bytes may remain at Google.' }; }
  catch { return job ? emit(job, 'uncertain', 'Transfer stopped locally; cancellation could not be confirmed. Refresh the operation.') : { operationId: id, phase: 'uncertain', confirmedBytes: 0, totalBytes: 0, message: 'Cancellation could not be confirmed. Refresh the operation.' }; }
}
export function dispose() {
  for (const job of jobs.values()) { job.control = 'pause'; job.receiver = null; job.controller?.abort(); job.capability = null; }
  jobs.clear();
}
