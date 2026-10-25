/** Metadata-only browser persistence. File handles and channel credentials never enter this module. */
export function read(key) { return localStorage.getItem(key); }
export function write(key, value) { localStorage.setItem(key, value); if (localStorage.getItem(key) !== value) throw new Error('The browser could not save this release. Download the project to keep it.'); }
export function rememberInvite(value) { if (value && value.length <= 200) sessionStorage.setItem('release-deck:pending-invite', value); }
export function recallInvite() { return sessionStorage.getItem('release-deck:pending-invite'); }
export function forgetInvite() { sessionStorage.removeItem('release-deck:pending-invite'); }
export async function readProject(input) { const file = input.files?.[0]; if (!file) return null; if (file.size > 524288) throw new Error('Choose a project file smaller than 512 KB.'); const text = await file.text(); input.value = ''; return text; }
export function download(name, text, mime) { const blob = new Blob([text], { type: mime }); deliver(name, blob); }
export function downloadBase64(name, text, mime) { const raw = atob(text), bytes = Uint8Array.from(raw, character => character.charCodeAt(0)); deliver(name, new Blob([bytes], { type: mime })); }
function deliver(name, blob) { const url = URL.createObjectURL(blob), link = document.createElement('a'); link.href = url; link.download = name; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000); }
