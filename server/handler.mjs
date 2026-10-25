import {ReleaseError,fail,response} from './rules.mjs';
import {workspaceRoutes} from './workspaces.mjs';
import {oauthRoutes,googleConfigured,cookieValue} from './google.mjs';
import {operationRoutes} from './operations.mjs';
const prefix='/api/commerce/release-deck/';
/** Host supplies the existing member/session/entitlement helpers; this product creates no parallel account system. */
export async function handleReleaseDeck({request,env},deps){
 try{
  if(!env.DB||!env.SITE_ORIGIN||new URL(request.url).origin!==env.SITE_ORIGIN)fail(503,'Release storage is unavailable.','configuration');
  if(!['GET','POST'].includes(request.method))fail(405,'Use GET or POST.');const url=new URL(request.url);if(!url.pathname.startsWith(prefix))fail(404,'This action is unavailable.');const parts=url.pathname.slice(prefix.length).split('/').filter(Boolean);if(parts.some(p=>p.length>100||/%|\.|\\/.test(p)))fail(404,'This action is unavailable.');
  const callback=parts.join('/')==='oauth/callback';if(request.method==='POST'&&request.headers.get('Origin')!==url.origin)fail(403,'Submit this action from Release Deck.','origin');
  const account=await deps.accountFromRequest(request,env.DB),c={request,env,db:env.DB,deps,parts,account,sessionHash:account?await deps.hash(cookieValue(request,'binsite_member')):null,recheck:async()=>{const current=await deps.accountFromRequest(request,env.DB);if(!current||current.id!==account?.id)fail(401,'Sign in again before continuing.','authentication');}};
  if((parts.length===0||parts.join('/')==='config')&&request.method==='GET')return response({configured:true,googleConfigured:googleConfigured(env),account:account?await deps.accountStatus(env.DB,account):null,limits:{freeBodyBytes:65536,paidBodyBytes:524288,maxEpisodes:40},providerSetupRequired:!googleConfigured(env)});
  if(!callback&&!account)fail(401,'Sign in to save or share this release.','authentication');if(account)await deps.limit(env.DB,account.id,'release-'+request.method,request.method==='GET'?180:60,60);
  if(callback||parts[0]==='oauth'||parts[0]==='connections'){const result=await oauthRoutes(c);if(result)return result;}
  if(!account)fail(401,'Sign in again.','authentication');
  const operation=await operationRoutes(c);if(operation){if(!(parts[0]==='operations'&&parts[2]==='capability'))await c.recheck();return operation;}
  const workspace=await workspaceRoutes(c);if(workspace){await c.recheck();return workspace;}
  fail(404,'This release action is unavailable.','not_found');
 }catch(error){if(error instanceof ReleaseError||Number.isInteger(error.status)&&error.status>=400&&error.status<=599)return response({error:error.message,code:error.code||'request_failed'},error.status);console.error('Release Deck action failed',{type:error?.name||'Error'});return response({error:'The action could not finish. Keep your draft and check the saved state.','code':'service_failure'},500);}
}
