(() => {
  'use strict';
  const nativeRaf = window.requestAnimationFrame.bind(window);
  const nativeTimeout = window.setTimeout.bind(window);
  const NativeDate = Date;
  let now = 0, serial = 0, seed = __RHMN_SEED__ >>> 0;
  const rafs = new Map(), timers = new Map(), errors = [];
  window.__RHMN_RENDERING__ = true;
  window.__rhmnErrors = errors;
  window.addEventListener('error', e => errors.push(e.message || 'Aset gagal dimuat'), true);
  window.addEventListener('unhandledrejection', e => errors.push(String(e.reason)));
  Object.defineProperty(performance, 'now', {value: () => now});
  window.Date = class extends NativeDate {
    constructor(...args) { super(...(args.length ? args : [1700000000000 + now])); }
    static now() { return 1700000000000 + now; }
  };
  Math.random = () => { seed = (Math.imul(seed,1664525)+1013904223) >>> 0; return seed / 4294967296; };
  window.requestAnimationFrame = fn => { const id = ++serial; rafs.set(id,fn); return id; };
  window.cancelAnimationFrame = id => rafs.delete(id);
  function schedule(fn,delay,args,repeat) {
    if(typeof fn !== 'function') throw new Error('Timer string tidak didukung; gunakan fungsi.');
    const id = ++serial, ms = Math.max(1,Number(delay)||0);
    timers.set(id,{fn,args,due:now+ms,repeat:repeat?ms:0}); return id;
  }
  window.setTimeout = (fn,ms,...args) => schedule(fn,ms,args,false);
  window.setInterval = (fn,ms,...args) => schedule(fn,ms,args,true);
  window.clearTimeout = window.clearInterval = id => timers.delete(id);
  const paint = () => new Promise((resolve,reject) => {
    const timer = nativeTimeout(() => reject(new Error('WebView2 tidak menggambar frame dalam 10 detik.')),10000);
    nativeRaf(() => nativeRaf(() => { window.__nativeClear(timer); resolve(); }));
  });
  // The original timeout is used only for paint/readiness deadlines.
  window.__rhmnReady = async () => {
    const assets = Promise.all([document.fonts.ready,...Array.from(document.images).map(img => {
      if(img.complete) {if(img.naturalWidth===0) throw new Error('Gambar gagal: '+img.src);return Promise.resolve();}
      return new Promise((res,rej)=>{img.addEventListener('load',res,{once:true});img.addEventListener('error',()=>rej(new Error('Gambar gagal: '+img.src)),{once:true});});
    })]);
    await Promise.race([assets,new Promise((_,reject)=>nativeTimeout(()=>reject(new Error('Aset belum siap setelah 30 detik.')),30000))]);
    await paint();
    return {contract:typeof window.renderAtTime==='function',canvas:Array.from(document.querySelectorAll('canvas')).map(c=>({width:c.width,height:c.height,cssWidth:c.clientWidth,cssHeight:c.clientHeight})),media:document.querySelectorAll('video,audio,iframe').length};
  };
  window.__rhmnFrame = async (seconds,mode) => {
    const target=seconds*1000;
    if(!Number.isFinite(target)||target<now) throw new Error('Waktu render harus maju.');
    const contract=mode==='contract'||(mode==='auto'&&typeof window.renderAtTime==='function');
    if(contract) {
      now=target;
      if(typeof window.renderAtTime!=='function') throw new Error('Proyek tidak memiliki window.renderAtTime(t).');
      await window.renderAtTime(seconds);
    } else {
      let steps=0;
      while(true) {
        let nextId=null,next=null;
        for(const [id,t] of timers) if(t.due<=target&&(!next||t.due<next.due)){next=t;nextId=id;}
        if(!next) break;
        if(++steps>10000) throw new Error('Terlalu banyak callback timer dalam satu frame.');
        now=next.due;
        if(next.repeat) next.due+=next.repeat; else timers.delete(nextId);
        next.fn(...next.args);
      }
      now=target;
      const callbacks=Array.from(rafs.entries());
      for(const [id,fn] of callbacks) { if(!rafs.has(id)) continue;rafs.delete(id);fn(target); }
    }
    for(const anim of document.getAnimations()) { anim.pause();anim.currentTime=target; }
    await paint();
    if(errors.length) throw new Error(errors.slice(0,5).join('\n'));
    return true;
  };
})();
