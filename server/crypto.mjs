import {fail} from './rules.mjs';
const encoder=new TextEncoder();
function bytes(value){try{if(/^[a-fA-F0-9]{64}$/.test(value))return Uint8Array.from(value.match(/../g),v=>parseInt(v,16));return Uint8Array.from(atob(value.replace(/-/g,'+').replace(/_/g,'/')),c=>c.charCodeAt(0));}catch{return new Uint8Array();}}
export function encryptionReady(env){return typeof env.TOKEN_ENCRYPTION_KEY==='string'&&bytes(env.TOKEN_ENCRYPTION_KEY).length===32;}
async function key(env,usage){if(!encryptionReady(env))fail(503,'Channel connection is unavailable.','configuration');return crypto.subtle.importKey('raw',bytes(env.TOKEN_ENCRYPTION_KEY),'AES-GCM',false,[usage]);}
const encode=value=>btoa(String.fromCharCode(...value)).replace(/\+/g,'-').replace(/\//g,'_').replace(/=+$/,'');
export async function seal(env,value,aad){const iv=crypto.getRandomValues(new Uint8Array(12)),data=await crypto.subtle.encrypt({name:'AES-GCM',iv,additionalData:encoder.encode(aad)},await key(env,'encrypt'),encoder.encode(JSON.stringify(value)));return '1.'+encode(iv)+'.'+encode(new Uint8Array(data));}
export async function open(env,value,aad){try{const[v,iv,data]=value.split('.');if(v!=='1')throw Error();const plain=await crypto.subtle.decrypt({name:'AES-GCM',iv:bytes(iv),additionalData:encoder.encode(aad)},await key(env,'decrypt'),bytes(data));return JSON.parse(new TextDecoder().decode(plain));}catch(error){if(error.status)throw error;fail(503,'The saved channel connection could not be opened. Reconnect it.','connection_unavailable');}}
