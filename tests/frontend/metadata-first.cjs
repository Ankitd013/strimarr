const fs=require('fs'),vm=require('vm'),assert=require('assert');
async function run(outcome){
 const handlers={},timers=[],nodes=[],button={disabled:false,dataset:{}};let refreshes=0;
 const page={classList:{contains:()=>false},querySelector:s=>s==='h1'?{textContent:'A Monster Calls'}:s==='.trackSelections'?{insertAdjacentElement(){}}:{},querySelectorAll:()=>[button],dispatchEvent:()=>refreshes++};
 const context={document:{addEventListener:(k,f)=>handlers[k]=f,createElement:()=>{const n={setAttribute(){},removeAttribute(){},addEventListener(){},append(){},style:{},remove(){}};nodes.push(n);return n}},window:{ApiClient:{getCurrentUserId:()=>1,getItem:async()=>({ProviderIds:{Stremio:'tt3416532',ClawizStreamStatus:outcome,ClawizHiddenVersions:'false'},MediaSources:[]})}},location:{hash:'#/details?id=test'},URLSearchParams,WeakMap,console,CustomEvent:class{},setTimeout:f=>{timers.push(f);return timers.length},clearTimeout(){}};
 vm.runInNewContext(fs.readFileSync(require('path').join(__dirname,'../../src/Strimarr/Frontend/js/metadata-first.js'),'utf8'),context);
 handlers.viewshow({target:page});await timers.shift()();await Promise.resolve();
 assert.equal(timers.length,0);assert.equal(button.disabled,true);assert.equal(refreshes,1);assert(nodes.some(n=>(n.textContent||'').includes('Show hidden versions')));assert(nodes.some(n=>outcome==='empty'?/No playable version/.test(n.textContent):/Source lookup failed/.test(n.textContent)));console.log(outcome+': hidden control available; terminal message; stale selector refreshed; polling stopped');
}
(async()=>{await run('empty');await run('error')})();
