const vm=require('node:vm'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const code=fs.readFileSync(path.join(__dirname,'../src/Rhmn.Studio/Assets/render-clock.js'),'utf8').replace('__RHMN_SEED__','12345');
function make(){
 const animations=[{pause(){this.paused=true;},currentTime:null}];
 const context=vm.createContext({setTimeout,clearTimeout,setInterval,clearInterval,requestAnimationFrame:fn=>setImmediate(()=>fn(0)),cancelAnimationFrame:clearImmediate,performance:{now:()=>100},addEventListener(){},document:{getAnimations:()=>animations},__nativeClear:clearTimeout});
 vm.runInContext('window=globalThis;',context);vm.runInContext(code,context);return {context,animations,run:s=>vm.runInContext(s,context)};
}
(async()=>{
 let count=0;const check=(v,m)=>{assert(v,m);count++;console.log('PASS:',m)};
 const a=make();check(a.run('window.__RHMN_RENDERING__')===true,'Render mode is set before project scripts');
 a.run('events=[]; setTimeout(()=>events.push(["timeout",performance.now(),Date.now()]),500); interval=setInterval(()=>events.push(["interval",performance.now()]),250); requestAnimationFrame(t=>events.push(["raf",t]));');
 await a.run('__rhmnFrame(.5,"clock")');
 check(a.run('events.length')===4,'Timers and RAF all execute at the expected frame');
 check(a.run('events[0][1]')===250,'Interval callback receives its scheduled time');
 check(a.run('events[1][2]')===1700000000500,'Date.now advances with virtual time');
 check(a.animations[0].paused&&a.animations[0].currentTime===500,'CSS animation is paused at the requested time');
 a.run('clearInterval(interval);events=[];requestAnimationFrame(()=>{events.push("first");cancelAnimationFrame(second);requestAnimationFrame(()=>events.push("next"));});second=requestAnimationFrame(()=>events.push("cancelled"));');
 await a.run('__rhmnFrame(.6,"clock")');check(a.run('events.join(",")')==='first','RAF cancellation within a frame is respected');
 await a.run('__rhmnFrame(.7,"clock")');check(a.run('events.join(",")')==='first,next','New RAF callback waits for the next output frame');
 await assert.rejects(a.run('__rhmnFrame(.6,"clock")'),/harus maju/);count++;console.log('PASS: Reverse time rejected');
 const b=make(),c=make();check(b.run('Math.random()')===c.run('Math.random()'),'Fixed random seed repeats across runs');
 b.run('renderAtTime=async t=>{contractTime=t;};requestAnimationFrame(()=>{throw Error("RAF should not run in contract mode")});');
 await b.run('__rhmnFrame(2,"auto")');check(b.run('contractTime')===2,'Automatic mode prefers and awaits renderAtTime');
 const d=make();await assert.rejects(d.run('__rhmnFrame(0,"contract")'),/tidak memiliki/);count++;console.log('PASS: Missing contract fails explicitly');
 console.log(count+' clock checks passed. Mock browser environment; no visual render claimed.');
})().catch(e=>{console.error(e);process.exitCode=1;});
