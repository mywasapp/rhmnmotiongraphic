const canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d');
window.renderAtTime=function(t){
 const w=innerWidth,h=innerHeight;if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}
 const p=t/12*Math.PI*2;ctx.clearRect(0,0,w,h);
 const bg=ctx.createLinearGradient(0,0,w,h);bg.addColorStop(0,'#061018');bg.addColorStop(1,'#113f42');ctx.fillStyle=bg;ctx.fillRect(0,0,w,h);
 for(let layer=0;layer<3;layer++){
  const col=['90,230,192','90,173,217','201,228,176'][layer];
  for(let line=0;line<58;line++){
   const f=line/57;ctx.beginPath();
   for(let x=-20;x<=w+20;x+=w/160){let n=x/w;let y=h*(0.30+layer*.15+Math.sin(n*4+p+layer*.8)*.13+Math.cos(n*7-p)*.04+(f-.5)*.16*Math.sin(n*2.2+1.2+p*.0));if(x===-20)ctx.moveTo(x,y);else ctx.lineTo(x,y);}
   ctx.strokeStyle=`rgba(${col},${(.06+.24*Math.sin(f*Math.PI)).toFixed(3)})`;ctx.lineWidth=Math.max(1,w/2200);ctx.stroke();
  }
 }
 const glow=ctx.createRadialGradient(w*.72,h*.25,0,w*.72,h*.25,w*.6);glow.addColorStop(0,'rgba(113,234,200,.09)');glow.addColorStop(1,'rgba(0,0,0,0)');ctx.fillStyle=glow;ctx.fillRect(0,0,w,h);
};
if(!window.__RHMN_RENDERING__){const start=performance.now();function play(now){renderAtTime((now-start)/1000);requestAnimationFrame(play);}requestAnimationFrame(play);}
