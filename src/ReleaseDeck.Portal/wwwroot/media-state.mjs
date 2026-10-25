const files = new Map();
export const identityAlgorithm = 'rd-sha256-tree-4m-v1';
export const fingerprintChunkBytes = 4 * 1024 * 1024;

export function requireMedia(id) {
  const entry = files.get(id);
  if (!entry) throw new Error('Select the local video again before continuing.');
  return entry;
}
export function registerMedia(file) {
  if (!(file instanceof Blob) || !Number.isSafeInteger(file.size) || file.size < 0 || typeof file.name !== 'string') throw new Error('Choose a local file.');
  const clientFileId = crypto.randomUUID();
  const entry = { clientFileId, file, url: URL.createObjectURL(file), lifecycle: new AbortController(), previews: new Set(), probe: null, metadata: null, metadataPromise: null, identity: null, hashJob: null, thumbnailQueue: Promise.resolve() };
  files.set(clientFileId, entry);
  return { clientFileId, name: file.name, size: file.size, lastModified: Number.isSafeInteger(file.lastModified) ? file.lastModified : 0, mimeType: file.type || 'application/octet-stream' };
}
export function forgetMedia(id) {
  const entry = files.get(id);
  if (!entry) return null;
  files.delete(id);
  entry.lifecycle.abort();
  entry.hashJob?.controller.abort();
  return entry;
}
export function allMediaIds() { return [...files.keys()]; }
export function normalizedIdentity(value) {
  const size = value?.byteLength ?? value?.size;
  if (value?.algorithm !== identityAlgorithm || !/^[a-f0-9]{64}$/.test(value?.fingerprint || '') || !Number.isSafeInteger(size) || size < 0) throw new Error('This file identity is not supported. Identify the selected file again.');
  return { algorithm: identityAlgorithm, fingerprint: value.fingerprint, byteLength: size };
}
export function sameIdentity(left, right) {
  const a = normalizedIdentity(left), b = normalizedIdentity(right);
  return a.algorithm === b.algorithm && a.byteLength === b.byteLength && a.fingerprint === b.fingerprint;
}
