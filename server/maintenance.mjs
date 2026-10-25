/** The host's hourly D1-only maintenance Worker invokes this; reads also reject expired records. */
export async function cleanupExpiredReleaseData(db,now=Date.now()){
 return db.batch([
  db.prepare('DELETE FROM release_oauth_states WHERE expires_at<=?').bind(now),
  db.prepare('DELETE FROM release_operations WHERE remote_expires_at<=? OR connection_id IN(SELECT id FROM release_connections WHERE expires_at<=?)').bind(now,now),
  db.prepare('DELETE FROM release_connections WHERE expires_at<=?').bind(now)
 ]);
}
